using System.Diagnostics;
using System.Security.Claims;
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

        public CandidateService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<Candidates> CreateAsync(Candidates data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingCandidate = await _context.Candidates.FirstOrDefaultAsync(x => x.IsDeleted == false && x.JobID == data.JobID && x.Email == data.Email && x.Status == "Applied");
                if (existingCandidate != null)
                {
                    existingCandidate.CandidateName = data.CandidateName;
                    existingCandidate.PhoneNumber = data.PhoneNumber;
                    existingCandidate.ResumeURL = data.ResumeURL; 
                    existingCandidate.PortfolioLink = data.PortfolioLink;
                    existingCandidate.Remarks = data.Remarks;
                    existingCandidate.Status = "Applied";
                    existingCandidate.ApplicationDate = DateTime.Now;

                    existingCandidate.DateUp = DateTime.Now;
                    existingCandidate.UserUp = data.UserIn;
                }
                else
                {
                    data.ApplicationDate = data.DateIn;
                    data.Status = "Applied";
                    data.ApplicantID = await _context.Applicants.Where(x => x.Email.ToLower() == data.Email.ToLower() && x.IsDeleted == false).Select(x => x.ApplicantID).FirstOrDefaultAsync();
                    await _context.Candidates.AddAsync(data);
                }             
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return existingCandidate ?? data;
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

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                string JobTitle = await _context.Jobs.Where(x => x.JobID == obj.JobID).Select(x => x.JobTitle).SingleOrDefaultAsync() ?? "";

                if (obj.IsScreening.HasValue && obj.IsScreeningUser.HasValue && !obj.IsAssessment.HasValue && !obj.IsInterview.HasValue && !obj.IsOffer.HasValue)
                {
                    if (obj.IsScreeningUser.Value)
                        SendAdvanceToNextPhaseEmail(obj.Email, obj.CandidateName, JobTitle, "Assessment");
                    if (!obj.IsScreeningUser.Value)
                        SendRejectionEmail(obj.Email, obj.CandidateName, JobTitle);
                }
                else if (obj.IsAssessment.HasValue && !obj.IsInterview.HasValue && !obj.IsOffer.HasValue)
                {
                    if (obj.IsAssessment.Value)
                        SendAdvanceToNextPhaseEmail(obj.Email, obj.CandidateName, JobTitle, "Interview");
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

        public async Task<ListResponse<Candidates>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from c in _context.Candidates
                            join j in _context.Jobs on c.JobID equals j.JobID
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
                                PortfolioLink = c.PortfolioLink
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
                                "jobid" => query.Where(x => x.JobID.Equals(value)),
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
              var result=  from c in _context.Candidates
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
                    PortfolioLink = c.PortfolioLink
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

        public void SendAdvanceToNextPhaseEmail(string toEmail, string candidateName, string jobTitle, string nextPhaseName)
        {
            string subject = $"Selamat! Anda Lolos ke Tahap Selanjutnya untuk Posisi {jobTitle}";
            string body = $@"
            <p>Dear {candidateName},</p>
            <p>Terima kasih atas partisipasi Anda dalam proses seleksi untuk posisi <strong>{jobTitle}</strong>.</p>
            <p>{(!string.IsNullOrEmpty(nextPhaseName) ? $"Kami ingin memberitahukan bahwa Anda telah berhasil lolos ke tahap selanjutnya, yaitu <strong>{nextPhaseName}</strong>. " : "")}Tim rekrutmen kami akan segera menghubungi Anda untuk penjadwalan lebih lanjut.</p>
            <p>Selamat dan persiapkan diri Anda dengan baik!</p>
            <p>Hormat kami,</p>
            <p>Tim Rekrutmen</p>";

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
