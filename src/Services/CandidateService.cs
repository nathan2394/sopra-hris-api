using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class CandidateService : IServiceJobsAsync<Candidates>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public CandidateService(EFContext context, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;
        public async Task<Candidates> CheckIfCandidateExists(long jobId, string email)
        {
            return await _context.Candidates
                .FirstOrDefaultAsync(x => x.IsDeleted == false && x.JobID == jobId && x.Email == email && x.Status == "Applied");
        }
        public async Task<Candidates> CreateAsync(Candidates data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.ApplicationDate = data.DateIn;
                data.Status = "Applied";
                data.ApplicantID = await _context.Applicants.Where(x => x.Email.ToLower() == data.Email.ToLower() && x.IsDeleted == false).Select(x => x.ApplicantID).FirstOrDefaultAsync();
                await _context.Candidates.AddAsync(data);
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                // Send assessment email if applicant conditions are met
                if (data.ApplicantID > 0)
                {
                    var applicant = await _context.Applicants.FirstOrDefaultAsync(x => x.ApplicantID == data.ApplicantID && x.IsDeleted == false);
                    if (applicant != null && applicant.ProfileCompletion == 10 && applicant.ConsentSignedAt != null)
                    {
                        var sql = @"SELECT j.*
                                FROM Jobs j
                                INNER JOIN JobTestTemplateOverrides jt on jt.JobID= j.JobID
                                where j.JobID=@JobID and j.IsDeleted=0 and jt.IsDeleted=0";
                        var job = await _context.Jobs.FromSqlRaw(sql, new SqlParameter("@JobID", data.JobID)).FirstOrDefaultAsync();
                        if (job != null)
                        {
                            await SendAssessmentEmailAsync(data.CandidateID, data.CandidateName, data.Email, job.JobTitle);
                        }
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        private async Task SendAssessmentEmailAsync(long candidateID, string candidateName, string email, string jobTitle)
        {
            try
            {
                var payload = new[]
                {
                    new
                    {
                        candidateID = candidateID,
                        candidateName = candidateName,
                        email = email,
                        job = jobTitle
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://ai.mixtra.id/webhook/sendAssesment", content);

                if (response.IsSuccessStatusCode)
                {
                    // Update Candidates record after successful email send
                    var candidate = await _context.Candidates.FirstOrDefaultAsync(x => x.CandidateID == candidateID && x.IsDeleted == false);
                    if (candidate != null)
                    {
                        candidate.IsScreeningTestEmailSent = true;
                        candidate.ScreeningTestEmailSentDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    Trace.WriteLine($"Failed to send assessment email. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error sending assessment email: {ex.Message}");
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);
            }
        }

        public async Task<(int successCount, int failureCount)> SendBlastAssessmentEmailAsync(long jobId, string dateRange)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                // Parse date range format: "2026-01-01|2026-01-31"
                DateTime startDate, endDate;
                
                if (string.IsNullOrWhiteSpace(dateRange) || !dateRange.Contains("|"))
                {
                    throw new ArgumentException("Date range must be in format 'yyyy-MM-dd|yyyy-MM-dd'");
                }

                var dates = dateRange.Split("|");
                if (dates.Length != 2 || 
                    !DateTime.TryParse(dates[0].Trim(), out startDate) || 
                    !DateTime.TryParse(dates[1].Trim(), out endDate))
                {
                    throw new ArgumentException("Invalid date format. Please use 'yyyy-MM-dd|yyyy-MM-dd'");
                }

                // Ensure end date includes the entire day
                endDate = endDate.Date.AddDays(1).AddSeconds(-1);

                // Query candidates matching jobId and ApplicationDate criteria
                // using SQL query as specified
                var sql = @"SELECT DISTINCT c.* 
                            FROM Candidates c
                            INNER JOIN Applicants a ON a.ApplicantID = c.ApplicantID
                            WHERE c.JobID = @JobID 
                            AND c.IsDeleted = 0 
                            AND a.IsDeleted = 0 
                            AND a.ProfileCompletion = 10 
                            AND a.ConsentSignedAt IS NOT NULL
                            AND ISNULL(c.IsScreeningTestEmailSent,0)=0
                            AND c.ApplicationDate >= @StartDate 
                            AND c.ApplicationDate <= @EndDate";

                var candidates = await _context.Candidates
                    .FromSqlRaw(sql, 
                        new SqlParameter("@JobID", jobId),
                        new SqlParameter("@StartDate", startDate),
                        new SqlParameter("@EndDate", endDate))
                    .Join(_context.Jobs, c => c.JobID, j => j.JobID, (c, j) => new { c, j })
                    .Select(x => new
                    {
                        x.c.CandidateID,
                        x.c.CandidateName,
                        x.c.Email,
                        x.j.JobTitle
                    })
                    .ToListAsync();

                if (!candidates.Any())
                {
                    await dbTrans.CommitAsync();
                    return (0, 0);
                }

                int successCount = 0;
                int failureCount = 0;
                
                // Process candidates in batches to avoid gateway timeout
                int batchSize = 50; // Send 50 candidates per batch
                var batches = candidates
                    .Select((candidate, index) => new { candidate, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.candidate).ToList())
                    .ToList();

                Trace.WriteLine($"Processing {candidates.Count} candidates in {batches.Count} batches of {batchSize} each");

                foreach (var batch in batches)
                {
                    // Prepare payload with array of candidates for this batch
                    var payload = batch.Select(c => new
                    {
                        candidateID = c.CandidateID,
                        candidateName = c.CandidateName,
                        email = c.Email,
                        job = c.JobTitle
                    }).ToList();

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    HttpResponseMessage response = null;

                    try
                    {
                        Trace.WriteLine($"Sending batch of {batch.Count} candidates to webhook");
                        response = await _httpClient.PostAsync("https://ai.mixtra.id/webhook/sendAssesment", content);
                    }
                    catch (TaskCanceledException ex)
                    {
                        Trace.WriteLine($"Timeout: Batch request timed out after {_httpClient.Timeout.TotalMinutes} minutes");
                        if (ex.StackTrace != null)
                            Trace.WriteLine(ex.StackTrace);
                        failureCount += batch.Count;
                        continue;
                    }
                    catch (HttpRequestException ex)
                    {
                        Trace.WriteLine($"HTTP Error in batch: {ex.Message}");
                        if (ex.StackTrace != null)
                            Trace.WriteLine(ex.StackTrace);
                        failureCount += batch.Count;
                        continue;
                    }

                    if (response != null && response.IsSuccessStatusCode)
                    {
                        try
                        {
                            // Parse response to get successful candidates
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var successfulCandidates = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(responseContent);

                            if (successfulCandidates != null && successfulCandidates.Count > 0)
                            {
                                // Extract CandidateIDs from response
                                var successfulCandidateIds = new List<long>();
                                foreach (var item in successfulCandidates)
                                {
                                    if (item.TryGetValue("CandidateID", out var idElement) && 
                                        long.TryParse(idElement.GetString(), out var candidateId))
                                    {
                                        successfulCandidateIds.Add(candidateId);
                                    }
                                }

                                // Update only the successful candidates
                                var candidatesToUpdate = await _context.Candidates
                                    .Where(x => successfulCandidateIds.Contains(x.CandidateID) && x.IsDeleted == false)
                                    .ToListAsync();

                                foreach (var candidate in candidatesToUpdate)
                                {
                                    candidate.IsScreeningTestEmailSent = true;
                                    candidate.ScreeningTestEmailSentDate = DateTime.Now;
                                }

                                await _context.SaveChangesAsync();
                                successCount += candidatesToUpdate.Count;
                                failureCount += batch.Count - candidatesToUpdate.Count;
                                
                                Trace.WriteLine($"Batch processed: {candidatesToUpdate.Count} successful, {batch.Count - candidatesToUpdate.Count} failed");
                            }
                            else
                            {
                                failureCount += batch.Count;
                                Trace.WriteLine($"No successful candidates in batch response");
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            Trace.WriteLine($"Error parsing batch response: {jsonEx.Message}");
                            failureCount += batch.Count;
                        }
                    }
                    else
                    {
                        Trace.WriteLine($"Failed to send batch. Status: {response?.StatusCode}");
                        if (response?.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                        {
                            Trace.WriteLine("Gateway timeout (504): Consider reducing batch size or optimizing webhook");
                        }
                        failureCount += batch.Count;
                    }
                }

                await dbTrans.CommitAsync();

                Trace.WriteLine($"Blast email completed: {successCount} successful, {failureCount} failed");
                return (successCount, failureCount);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error sending blast assessment emails: {ex.Message}");
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<bool> UpdateCandidatesEmailSentStatusAsync(List<long> candidateIds)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                if (candidateIds == null || candidateIds.Count == 0)
                    return false;

                var candidatesToUpdate = await _context.Candidates
                    .Where(x => candidateIds.Contains(x.CandidateID) && x.IsDeleted == false)
                    .ToListAsync();

                if (candidatesToUpdate.Count == 0)
                    return false;

                foreach (var candidate in candidatesToUpdate)
                {
                    candidate.IsScreeningTestEmailSent = true;
                    candidate.ScreeningTestEmailSentDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error updating candidates email sent status: {ex.Message}");
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Candidates.FirstOrDefaultAsync(x => x.CandidateID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<Candidates> EditAsync(Candidates data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Candidates.FirstOrDefaultAsync(x => x.CandidateID == data.CandidateID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.IsScreening = data.IsScreening;
                obj.ScreeningDate = data.ScreeningDate;
                obj.ScreeningBy = data.ScreeningBy;
                obj.ScreeningNotes = data.ScreeningNotes;
                obj.IsScreeningUser = data.IsScreeningUser;
                obj.ScreeningUserDate = data.ScreeningUserDate;
                obj.ScreeningUserBy = data.ScreeningUserBy;
                obj.ScreeningUserNotes = data.ScreeningUserNotes;
                obj.IsAssessment = data.IsAssessment;
                obj.AssessmentBy = data.AssessmentBy;
                obj.AssessmentDate = data.AssessmentDate;
                obj.AssessmentResult = data.AssessmentResult;
                obj.IsInterview = data.IsInterview;
                obj.InterviewBy = data.InterviewBy;
                obj.InterviewDate = data.InterviewDate;
                obj.InterviewResult = data.InterviewResult;
                obj.IsOffer = data.IsOffer;
                obj.OfferBy = data.OfferBy;
                obj.OfferDate = data.OfferDate;
                obj.OfferResult = data.OfferResult;
                obj.Status = data.Status;
                obj.ApplicantID = data.ApplicantID;
                obj.InterviewMethod = data.InterviewMethod;
                obj.InterviewLink = data.InterviewLink;
                obj.InterviewLocation = data.InterviewLocation;
                obj.FitScore = data.FitScore;
                obj.GradeLevel = data.GradeLevel;
                obj.AIRecommendationSummary = data.AIRecommendationSummary;
                obj.AssessmentTestLink = data.AssessmentTestLink;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                string JobTitle = await _context.Jobs.Where(x => x.JobID == obj.JobID).Select(x => x.JobTitle).SingleOrDefaultAsync() ?? "";

                if (obj.IsScreening.HasValue && !obj.IsScreeningUser.HasValue && !obj.IsAssessment.HasValue && !obj.IsInterview.HasValue && !obj.IsOffer.HasValue)
                {
                    if (!obj.IsScreening.Value)
                        SendRejectionEmail(obj.Email, obj.CandidateName, JobTitle);
                }
                else if (obj.IsScreeningUser.HasValue && !obj.IsAssessment.HasValue && !obj.IsInterview.HasValue && !obj.IsOffer.HasValue)
                {
                    if (obj.IsScreeningUser.Value)
                    {
                        var jobs = await _context.Jobs.Where(x => x.JobID == obj.JobID).FirstOrDefaultAsync();
                        SendAdvanceToNextPhaseEmail(obj.Email, obj.CandidateName, JobTitle, "Assessment", jobs.PsychotestLink ?? "", data.AssessmentDate);
                    }
                    if (!obj.IsScreeningUser.Value)
                        SendRejectionEmail(obj.Email, obj.CandidateName, JobTitle);
                }
                else if (obj.IsAssessment.HasValue && !obj.IsInterview.HasValue && !obj.IsOffer.HasValue)
                {
                    if (obj.IsAssessment.Value)
                        SendAdvanceToNextPhaseEmail(obj.Email, obj.CandidateName, JobTitle, "Interview", "", data.InterviewDate, data.InterviewMethod, data.InterviewLink, data.InterviewLocation);
                    if (!obj.IsAssessment.Value)
                        SendRejectionEmail(obj.Email, obj.CandidateName, JobTitle);
                }
                else if (obj.IsInterview.HasValue && !obj.IsOffer.HasValue)
                {
                    if (obj.IsInterview.Value)
                        SendAdvanceToNextPhaseEmail(obj.Email, obj.CandidateName, JobTitle, "Offering");
                    if (!obj.IsInterview.Value)
                        SendRejectionEmail(obj.Email, obj.CandidateName, JobTitle);
                }
                else if (obj.IsOffer.HasValue)
                {
                    if (obj.IsOffer.Value)
                        SendAdvanceToNextPhaseEmail(obj.Email, obj.CandidateName, JobTitle, "");
                    if (!obj.IsOffer.Value)
                        SendRejectionEmail(obj.Email, obj.CandidateName, JobTitle);
                }

                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }
        public async Task<ListResponse<CandidateDTO>> GetAllCustomAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                var offset = page * limit;
                var sortParts = string.IsNullOrEmpty(sort) ? new string[0] : sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var sortBy = sortParts.Length > 0 ? sortParts[0].ToLower() : null;
                var sortOrder = sortParts.Length > 1 && sortParts[1].ToLower() == "desc" ? "DESC" : "ASC";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@CandidateID", SqlDbType.BigInt) { Value = 0 },
                    new SqlParameter("@FullName", SqlDbType.NVarChar, 200) { Value = DBNull.Value },
                    new SqlParameter("@JobTitle", SqlDbType.NVarChar, 200) { Value = DBNull.Value },
                    new SqlParameter("@JobID", SqlDbType.Int) { Value = DBNull.Value },
                    new SqlParameter("@MobilePhoneNumber", SqlDbType.NVarChar, 50) { Value = DBNull.Value },
                    new SqlParameter("@Email", SqlDbType.NVarChar, 200) { Value = DBNull.Value },
                    new SqlParameter("@Status", SqlDbType.NVarChar, 50) { Value = DBNull.Value },
                    new SqlParameter("@Department", SqlDbType.NVarChar, 100) { Value = DBNull.Value },
                    new SqlParameter("@Location", SqlDbType.NVarChar, 100) { Value = DBNull.Value },
                    new SqlParameter("@JobType", SqlDbType.NVarChar, 50) { Value = DBNull.Value },
                    new SqlParameter("@ApplicantID", SqlDbType.BigInt) { Value = DBNull.Value },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = DBNull.Value },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = DBNull.Value },
                    new SqlParameter("@Limit", SqlDbType.Int) { Value = 1000 },
                    new SqlParameter("@Offset", SqlDbType.Int) { Value = 0 },
                    new SqlParameter("@SortBy", SqlDbType.NVarChar, 50) { Value = DBNull.Value },
                    new SqlParameter("@SortOrder", SqlDbType.NVarChar, 4) { Value = "ASC" },
                    new SqlParameter("@UserID", SqlDbType.BigInt) { Value = UserID}
                };

                // Apply filters from 'filter' string
                if (!string.IsNullOrEmpty(filter))
                {
                    var filters = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var f in filters)
                    {
                        var parts = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].ToLower();
                            var val = parts[1];

                            switch (key)
                            {
                                case "candidateid":
                                    if (long.TryParse(val, out var id))
                                        parameters.First(p => p.ParameterName == "@CandidateID").Value = id;
                                    break;
                                case "name":
                                    parameters.First(p => p.ParameterName == "@FullName").Value = val;
                                    break;
                                case "jobtitle":
                                    parameters.First(p => p.ParameterName == "@JobTitle").Value = val;
                                    break;
                                case "jobid":
                                    if (int.TryParse(val, out var jobId))
                                        parameters.First(p => p.ParameterName == "@JobID").Value = jobId;
                                    break;
                                case "phonenumber":
                                    parameters.First(p => p.ParameterName == "@MobilePhoneNumber").Value = val;
                                    break;
                                case "email":
                                    parameters.First(p => p.ParameterName == "@Email").Value = val;
                                    break;
                                case "status":
                                    parameters.First(p => p.ParameterName == "@Status").Value = val;
                                    break;
                                case "department":
                                    parameters.First(p => p.ParameterName == "@Department").Value = val;
                                    break;
                                case "location":
                                    parameters.First(p => p.ParameterName == "@Location").Value = val;
                                    break;
                                case "jobtype":
                                    parameters.First(p => p.ParameterName == "@JobType").Value = val;
                                    break;
                                case "applicant":
                                    if (long.TryParse(val, out var appId))
                                        parameters.First(p => p.ParameterName == "@ApplicantID").Value = appId;
                                    break;
                            }
                        }
                    }
                }

                // Apply date filter
                if (!string.IsNullOrEmpty(date))
                {
                    var dates = date.Split("|");
                    if (dates.Length == 2 && DateTime.TryParse(dates[0], out var startDate) && DateTime.TryParse(dates[1], out var endDate))
                    {
                        parameters.First(p => p.ParameterName == "@StartDate").Value = startDate;
                        parameters.First(p => p.ParameterName == "@EndDate").Value = endDate;
                    }
                }

                var data = await _context.CandidateDTO
    .FromSqlRaw("EXEC GetCandidateFullProfile @CandidateID, @FullName, @JobTitle, @JobID, @MobilePhoneNumber, @Email, @Status, @Department, @Location, @JobType, @ApplicantID, @StartDate, @EndDate, @Limit, @Offset, @SortBy, @SortOrder, @UserID", parameters)
    .ToListAsync();


                total = data.Count; // Optional: or you can call a separate COUNT SP

                return new ListResponse<CandidateDTO>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<ListResponse<Candidates>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from c in _context.Candidates
                            join j in _context.Jobs on c.JobID equals j.JobID
                            join a in _context.Applicants on c.ApplicantID equals a.ApplicantID into aGroup
                            from a in aGroup.DefaultIfEmpty()
                            where c.IsDeleted == false
                            select new Candidates
                            {
                                CandidateID = c.CandidateID,
                                CandidateName = c.CandidateName,
                                JobID = c.JobID,
                                JobTitle = j.JobTitle,
                                Email = c.Email,
                                PhoneNumber = c.PhoneNumber,
                                ResumeURL = c.ResumeURL,
                                Remarks = c.Remarks,
                                ApplicationDate = c.ApplicationDate,
                                ApplicantID = c.ApplicantID,
                                IsScreening = c.IsScreening,
                                ScreeningBy = c.ScreeningBy,
                                ScreeningDate = c.ScreeningDate,
                                ScreeningNotes = c.ScreeningNotes,
                                IsScreeningUser = c.IsScreeningUser,
                                ScreeningUserBy = c.ScreeningUserBy,
                                ScreeningUserDate = c.ScreeningUserDate,
                                ScreeningUserNotes = c.ScreeningUserNotes,
                                IsAssessment = c.IsAssessment,
                                AssessmentBy = c.AssessmentBy,
                                AssessmentDate = c.AssessmentDate,
                                AssessmentResult = c.AssessmentResult,
                                IsInterview = c.IsInterview,
                                InterviewBy = c.InterviewBy,
                                InterviewDate = c.InterviewDate,
                                InterviewResult = c.InterviewResult,
                                IsOffer = c.IsOffer,
                                OfferBy = c.OfferBy,
                                OfferDate = c.OfferDate,
                                OfferResult = c.OfferResult,
                                Status = c.Status,
                                OtpVerify = c.OtpVerify,
                                CompanyID = j.CompanyID,
                                Location = j.Location,
                                Department = j.Department,
                                JobType = j.JobType,
                                PortfolioLink = c.PortfolioLink,
                                PsychotestLink = j.PsychotestLink,
                                InterviewLink = c.InterviewLink,
                                InterviewLocation = c.InterviewLocation,
                                InterviewMethod = c.InterviewMethod,
                                FitScore = c.FitScore,
                                GradeLevel = c.GradeLevel,
                                AIRecommendationSummary = c.AIRecommendationSummary,
                                AssessmentTestLink = c.AssessmentTestLink,
                                JobUsers = j.JobUsers,
                                ConsentSignedAt = a.ConsentSignedAt,
                                ProfileCompletion = a.ProfileCompletion
                            };

                // Searching 
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.JobTitle.Contains(search) || x.CandidateName.Contains(search) || x.Department.Contains(search) || x.Location.Contains(search) || x.JobType.Contains(search)
                        );

                // Filtering
                if (!string.IsNullOrEmpty(filter))
                {
                    var filterList = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var f in filterList)
                    {
                        var searchList = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        if (searchList.Length == 2)
                        {
                            var fieldName = searchList[0].Trim().ToLower();
                            var value = searchList[1].Trim();
                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.CandidateName.Contains(value)),
                                "jobtitle" => query.Where(x => x.JobTitle.Contains(value)),
                                "jobid" => long.TryParse(value, out var JobID) ? query.Where(x => x.JobID == JobID) : query,
                                "phonenumber" => query.Where(x => x.PhoneNumber.Contains(value)),
                                "email" => query.Where(x => x.Email.Contains(value)),
                                "status" => query.Where(x => x.Status.Contains(value)),
                                "department" => query.Where(x => x.Department.Contains(value)),
                                "location" => query.Where(x => x.Location.Contains(value)),
                                "jobtype" => query.Where(x => x.JobType.Contains(value)),
                                "applicant" => long.TryParse(value, out var applicantId) ? query.Where(x => x.ApplicantID == applicantId) : query,
                                _ => query
                            };
                        }
                    }
                }

                long effectiveRoleId;
                if (UserID > 0 && RoleID > 0)
                {
                    if (UserID == 266 || UserID == 267)
                        effectiveRoleId = 1;
                    else if (RoleID == 3 || RoleID == 4)
                        effectiveRoleId = 1;
                    else if (RoleID == 8)
                        effectiveRoleId = 10;
                    else
                        effectiveRoleId = RoleID;

                    if (effectiveRoleId == 1 || effectiveRoleId == 10)
                    {
                        if (effectiveRoleId == 10)
                            query = query.Where(x =>
                            x.JobUsers != null &&
                            (
                                EF.Functions.Like(x.JobUsers, $"{UserID},%") ||
                                EF.Functions.Like(x.JobUsers, $"%,{UserID},%") ||
                                EF.Functions.Like(x.JobUsers, $"%,{UserID}") ||
                                x.JobUsers == UserID.ToString()
                            )
                        );
                    }
                    else
                    {
                        query = query.Where(x => false);
                    }

                    if (UserID > 0)
                    {
                        query = query.Where(x => x.ConsentSignedAt != null && x.ProfileCompletion == 10);
                    }
                }


                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                {
                    var dateRange = date.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    if (dateRange.Length == 2 && DateTime.TryParse(dateRange[0], out var startDate) && DateTime.TryParse(dateRange[1], out var endDate))
                        query = query.Where(x => (x.ApplicationDate.Value.Date >= startDate.Date && x.ApplicationDate.Value.Date <= endDate.Date));
                }

                // Sorting
                if (!string.IsNullOrEmpty(sort))
                {
                    var temp = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var orderBy = sort;
                    if (temp.Length > 1)
                        orderBy = temp[0];

                    if (temp.Length > 1)
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderByDescending(x => x.CandidateName),
                            "jobtitle" => query.OrderByDescending(x => x.JobTitle),
                            "jobid" => query.OrderByDescending(x => x.JobID),
                            "phonenumber" => query.OrderByDescending(x => x.PhoneNumber),
                            "email" => query.OrderByDescending(x => x.Email),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.CandidateName),
                            "jobtitle" => query.OrderBy(x => x.JobTitle),
                            "jobid" => query.OrderBy(x => x.JobID),
                            "phonenumber" => query.OrderBy(x => x.PhoneNumber),
                            "email" => query.OrderBy(x => x.Email),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.CandidateID);
                }

                // Get Total Before Limit and Page
                total = await query.CountAsync();

                // Set Limit and Page
                if (limit != 0)
                    query = query.Skip(page * limit).Take(limit);

                // Get Data
                var data = await query.ToListAsync();
                if (data.Count <= 0 && page > 0)
                {
                    page = 0;
                    return await GetAllAsync(limit, page, total, search, sort, filter, date);
                }

                return new ListResponse<Candidates>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Candidates> GetByIdAsync(long id)
        {
            try
            {
                var result = from c in _context.Candidates
                             join j in _context.Jobs on c.JobID equals j.JobID
                             where c.IsDeleted == false && c.CandidateID == id
                             select new Candidates
                             {
                                 CandidateID = c.CandidateID,
                                 CandidateName = c.CandidateName,
                                 JobID = c.JobID,
                                 JobTitle = j.JobTitle,
                                 Email = c.Email,
                                 PhoneNumber = c.PhoneNumber,
                                 ResumeURL = c.ResumeURL,
                                 Remarks = c.Remarks,
                                 ApplicationDate = c.ApplicationDate,
                                 ApplicantID = c.ApplicantID,
                                 IsScreening = c.IsScreening,
                                 ScreeningBy = c.ScreeningBy,
                                 ScreeningDate = c.ScreeningDate,
                                 ScreeningNotes = c.ScreeningNotes,
                                 IsScreeningUser = c.IsScreeningUser,
                                 ScreeningUserBy = c.ScreeningUserBy,
                                 ScreeningUserDate = c.ScreeningUserDate,
                                 ScreeningUserNotes = c.ScreeningUserNotes,
                                 IsAssessment = c.IsAssessment,
                                 AssessmentBy = c.AssessmentBy,
                                 AssessmentDate = c.AssessmentDate,
                                 AssessmentResult = c.AssessmentResult,
                                 IsInterview = c.IsInterview,
                                 InterviewBy = c.InterviewBy,
                                 InterviewDate = c.InterviewDate,
                                 InterviewResult = c.InterviewResult,
                                 IsOffer = c.IsOffer,
                                 OfferBy = c.OfferBy,
                                 OfferDate = c.OfferDate,
                                 OfferResult = c.OfferResult,
                                 Status = c.Status,
                                 OtpVerify = c.OtpVerify,
                                 CompanyID = j.CompanyID,
                                 Location = j.Location,
                                 Department = j.Department,
                                 JobType = j.JobType,
                                 PortfolioLink = c.PortfolioLink,
                                 PsychotestLink = j.PsychotestLink,
                                 InterviewLink = c.InterviewLink,
                                 InterviewLocation = c.InterviewLocation,
                                 InterviewMethod = c.InterviewMethod,
                                 GradeLevel = c.GradeLevel,
                                 FitScore = c.FitScore,
                                 AIRecommendationSummary = c.AIRecommendationSummary,
                                 AssessmentTestLink = c.AssessmentTestLink
                             };
                return await result.AsNoTracking().FirstOrDefaultAsync();                
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Dictionary<string, object>> GetFilters(string filter)
        {
            throw new NotImplementedException();
        }

        public async Task<string> SaveOTPToDatabase(string Name, string Email)
        {
            try
            {
                var checkAccountQuery = @"
            SELECT TOP 1 1
            FROM Applicants
            WHERE Email = @Email AND IsDeleted = 0";

                bool userExists = false;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = checkAccountQuery;
                    command.Parameters.Add(new SqlParameter("@Email", Email));

                    _context.Database.OpenConnection();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        userExists = await reader.ReadAsync();
                    }
                }

                // If user doesn't exist, return false or handle the case
                if (userExists)               
                    return "Account found.";
                
                var rateLimitCheckQuery = @"
            SELECT TOP 1 RequestCount, LastRequestTime
            FROM OTPVerification
            WHERE Email = @Email AND IsVerify = 0
            ORDER BY LastRequestTime DESC";

                int requestCount = 0;
                DateTime? lastRequestTime = null;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = rateLimitCheckQuery;
                    command.Parameters.Add(new SqlParameter("@Email", Email));

                    _context.Database.OpenConnection();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            requestCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            lastRequestTime = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                        }
                    }
                }

                // Validasi rate limit
                if (lastRequestTime != null && lastRequestTime > DateTime.Now.AddHours(-1) && requestCount >= 5)
                    return "Too many OTP requests in the last hour.";
                
                string otp = new Random().Next(1000, 9999).ToString();
                DateTime expirationDate = DateTime.Now.AddMinutes(10);

                var query = @"
                IF EXISTS (SELECT 1 FROM OTPVerification WHERE Email = @Email AND IsVerify = 0)
                BEGIN
                    UPDATE OTPVerification
                    SET OTP = @OTP, ExpirationDate = @ExpirationDate, IsVerify = 0,
                        RequestCount = ISNULL(RequestCount, 0) + 1,
                        LastRequestTime = GETDATE()
                    WHERE Email = @Email AND IsVerify = 0
                END
                ELSE
                BEGIN
                    INSERT INTO OTPVerification (Email, OTP, ExpirationDate, IsVerify, RequestCount, LastRequestTime)
                    VALUES (@Email, @OTP, @ExpirationDate, 0, 1, GETDATE())
                END";

                var result = await _context.Database.ExecuteSqlRawAsync(query,
                    new SqlParameter("@Email", Email),
                    new SqlParameter("@OTP", otp),
                    new SqlParameter("@ExpirationDate", expirationDate));

                try
                {
                    string subject = "Verifikasi OTP untuk Akun Anda";
                    string body = $@"<!DOCTYPE html>
<html lang=""id"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verifikasi Akun</title>
</head>
<body>
    <p>Dear {Name},</p>

    <p>Untuk menyelesaikan proses verifikasi akun Anda di <strong>SOPRA Group</strong>, masukkan One-Time Password (OTP) berikut:</p>

    <p><strong>OTP Anda:</strong> {otp}</p>

    <p>OTP ini hanya berlaku selama 10 menit. Jangan berbagi kode ini dengan orang lain.</p>

    <p>Jika Anda tidak melakukan permintaan ini, Anda dapat mengabaikan email ini.</p>

    <p>Terima kasih</p>

</body>
</html>
";

                    Utility.sendMail(Email, "", subject, body);

                    return result == 1 ? "OTP has been sent to your email." : "Failed to send OTP.";
                }
                catch (Exception ex)
                {
                    return $"Error sending email: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);
                return $"Error: {ex.Message}";
            }
            finally
            {
                _context.Dispose();
            }
        }

        public async Task<bool> VerifyOTP(string email, string inputOtp)
        {
            try
            {
                string query = @"
                        SELECT COUNT(1) Value
                        FROM OTPVerification
                        WHERE Email = @Email AND OTP = @OTP
                            AND ExpirationDate > GETDATE()
                            AND IsVerify = 0";

                var isValid = await _context.Database
                           .SqlQueryRaw<int>(query,
                                          new SqlParameter("@Email", email),
                                          new SqlParameter("@OTP", inputOtp))
                           .FirstOrDefaultAsync();

                if (isValid != 1)
                    return false;

                string updateQuery = @"
                    UPDATE OTPVerification
                    SET IsVerify = 1
                    WHERE Email = @Email AND OTP = @OTP AND IsVerify = 0";

                var result = await _context.Database.ExecuteSqlRawAsync(updateQuery,
                    new SqlParameter("@Email", email),
                    new SqlParameter("@OTP", inputOtp));
                return result == 1;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);
            }
            finally
            {
                _context.Dispose();
            }
            return false;
        }
        public async Task<CandidateSummaryEmailListResponse> GetDailySummaryEmailAsync(string date = "")
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                DateTime summaryDate = string.IsNullOrEmpty(date) ? DateTime.Now.Date : DateTime.Parse(date).Date;

                // Query summary data using LINQ
                var summary = await (from c in _context.Candidates
                                     join j in _context.Jobs on c.JobID equals j.JobID
                                     join a in _context.Applicants on c.ApplicantID equals a.ApplicantID
                                     where c.IsDeleted == false
                                       && c.DateUp.Value.Date == summaryDate
                                       && a.IsDeleted == false
                                       && a.ProfileCompletion == 10
                                       && a.ConsentSignedAt != null
                                     group new { c, j } by new { j.JobTitle, c.Status, j.Location } into grp
                                     select new CandidateSummaryResponse
                                     {
                                         JobTitle = grp.Key.JobTitle,
                                         Status = grp.Key.Status,
                                         KandidatCount = grp.Count(),
                                         Location = grp.Key.Location
                                     })
                    .OrderBy(x => x.JobTitle)
                    .ThenBy(x => x.Status)
                    .ToListAsync();

                // Query job users data
                var jobUsersData = await (from c in _context.Candidates
                                          join j in _context.Jobs on c.JobID equals j.JobID
                                          join a in _context.Applicants on c.ApplicantID equals a.ApplicantID
                                          where c.IsDeleted == false
                                            && c.DateUp.Value.Date == summaryDate
                                            && a.IsDeleted == false
                                            && a.ProfileCompletion == 10
                                            && a.ConsentSignedAt != null
                                          select new { j.JobTitle, j.JobUsers })
                    .Distinct()
                    .ToListAsync();

                var userEmails = new List<CandidateSummaryUserEmailResponse>();

                foreach (var jobData in jobUsersData)
                {
                    if (string.IsNullOrEmpty(jobData.JobUsers))
                        continue;

                    // Parse JobUsers string (comma-separated UserIDs)
                    var userIdStrings = jobData.JobUsers.Split(',')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    foreach (var userIdStr in userIdStrings)
                    {
                        if (long.TryParse(userIdStr, out var userId))
                        {
                            var user = await _context.Users
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.IsDeleted == false && u.UserID == userId);

                            if (user != null && !string.IsNullOrEmpty(user.Email))
                            {
                                userEmails.Add(new CandidateSummaryUserEmailResponse
                                {
                                    Name = user.Name,
                                    Email = user.Email,
                                    JobTitle = jobData.JobTitle
                                });
                            }
                        }
                    }
                }

                var sortedUserEmails = userEmails
                    .OrderBy(x => x.Email)
                    .ThenBy(x => x.JobTitle)
                    .ToList();

                return new CandidateSummaryEmailListResponse(summary, sortedUserEmails);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public void SendAdvanceToNextPhaseEmail(string toEmail, string candidateName, string jobTitle, string nextPhaseName, string PsychotestLink = "", DateTime? TaskDate = null, string interviewMethod = "", string interviewLink = "", string interviewLocation = "")
        {
            string subject = $"Selamat! Anda Lolos ke Tahap Selanjutnya untuk Posisi {jobTitle}";       
            string body = $@"
        <p>Dear {candidateName},</p>
        <p>Terima kasih atas partisipasi Anda dalam proses seleksi untuk posisi <strong>{jobTitle}</strong>.</p>";

            // Next phase message
            if (!string.IsNullOrEmpty(nextPhaseName))
            {
                body += $@"
        <p>Kami ingin memberitahukan bahwa Anda telah berhasil lolos ke tahap selanjutnya, yaitu <strong>{nextPhaseName}</strong>.</p>";

                // Additional info for specific phases
                if (nextPhaseName.Equals("Assessment", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(PsychotestLink))
                    {
                        body += $@"<p>Silakan akses link berikut untuk mengikuti psikotes: <a href='{PsychotestLink}' target='_blank'>click here</a></p>";
                    }

                    if (jobTitle == "Backend Engineer")
                    {
                        body += @"<p>Silahkan akses link berikut untuk mengikut technical test 1: <a href='https://forms.gle/Quo3SSF9RwsxRJFZ7' target='_blank'>click here</a></p>";
                        body += @"<p>Silahkan akses link berikut untuk mengikut technical test 2: <a href='https://forms.gle/drfGUFPh2zGSwjiW7' target='_blank'>click here</a></p>";
                    }
                    if (jobTitle == "Junior AI & Data Engineer")
                    {
                        body += @"<p>Silahkan akses link berikut untuk mengikut technical test : <a href='https://drive.google.com/drive/folders/1E_yzZcNHie9YY9GSslRm7MmMn42vXT6Q?usp=sharing' target='_blank'>click here</a></p>";                        
                    }

                    if (TaskDate.HasValue)
                    {
                        body += $@"<p>Deadline: <strong>{TaskDate.Value:dd MMMM yyyy}</strong></p>";
                    }
                }
                else if (nextPhaseName.Equals("Interview", StringComparison.OrdinalIgnoreCase))
                {
                    if (TaskDate.HasValue)
                    {
                        body += $@"<p>Jadwal interview Anda: <strong>{TaskDate.Value:dd MMMM yyyy HH:mm}</strong></p>";
                    }
                    if (!string.IsNullOrEmpty(interviewMethod))
                    {
                        if (interviewMethod.Equals("Online", StringComparison.OrdinalIgnoreCase))
                        {
                            body += $@"
<p>Interview akan dilaksanakan secara <strong>Online</strong>.</p>
<p>Silakan bergabung melalui link berikut pada waktu yang telah ditentukan:
<br><a href='{interviewLink}' target='_blank'>click here</a></p>";
                        }
                        else if (interviewMethod.Equals("Offline", StringComparison.OrdinalIgnoreCase))
                        {
                            body += $@"
<p>Interview akan dilaksanakan secara <strong>Offline</strong>.</p>
<p>Lokasi interview:
<br><strong>{interviewLocation}</strong></p>
<p>Kami sarankan untuk hadir 10-15 menit lebih awal.</p>";
                        }
                    }
                }
                else
                {
                    body += "<p>Tim rekrutmen kami akan segera menghubungi Anda untuk penjadwalan lebih lanjut.</p>";
                }
            }
            else
            {
                body += @"<p>Kami ingin memberitahukan bahwa Anda telah melewati seluruh proses seleksi dan tim rekrutmen kami akan menghubungi Anda untuk proses selanjutnya.</p>";
            }

            // Closing
            body += @"
        <p>Selamat dan persiapkan diri Anda dengan baik!</p>
        <p>Hormat kami,</p>
        <p>Tim Rekrutmen</p>";

            // Send email
            Utility.sendMail(toEmail, "", subject, body);
        }

        public void SendRejectionEmail(string toEmail, string candidateName, string jobTitle)
        {
            string subject = $"Update Mengenai Lamaran Anda untuk Posisi {jobTitle}";
            string body = $@"
            <p>Dear {candidateName},</p>
            <p>Terima kasih atas waktu dan usaha yang telah Anda berikan dalam proses seleksi untuk posisi <strong>{jobTitle}</strong>.</p>
            <p>Setelah melalui pertimbangan yang saksama, kami harus memberitahukan bahwa kami memutuskan untuk melanjutkan dengan kandidat lain yang kualifikasinya lebih sesuai dengan kebutuhan kami saat ini.</p>
            <p>Kami sangat menghargai minat Anda untuk bergabung dengan perusahaan kami dan kami akan menyimpan data Anda untuk kesempatan di masa depan. Kami doakan yang terbaik untuk karir Anda selanjutnya.</p>
            <p>Hormat kami,</p>
            <p>Tim Rekrutmen</p>";

            Utility.sendMail(toEmail, "", subject, body);
        }
    }
}
