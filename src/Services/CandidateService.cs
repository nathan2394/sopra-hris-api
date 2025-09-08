using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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
                data.ApplicationDate = data.DateIn;
                data.Status = "Apply";
                data.ApplicantID = await _context.Applicants.Where(x => x.Email.ToLower() == data.Email.ToLower() && x.IsDeleted == false).Select(x => x.ApplicantID).FirstOrDefaultAsync();
               
                await _context.Candidates.AddAsync(data);
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

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
                obj.IsAssessment = data.IsAssessment;
                obj.AssessmentDate = data.AssessmentDate;
                obj.AssessmentResult = data.AssessmentResult;
                obj.IsInterview = data.IsInterview;
                obj.InterviewDate = data.InterviewDate;
                obj.InterviewResult = data.InterviewResult;
                obj.Status = data.Status;
                obj.ApplicantID = data.ApplicantID;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

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

        public Task<ListResponse<Candidates>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            throw new NotImplementedException();
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
                                ScreeningNotes = c.ScreeningNotes,
                                IsAssessment = c.IsAssessment,
                                AssessmentDate = c.AssessmentDate,
                                AssessmentResult = c.AssessmentResult,
                                IsInterview = c.IsInterview,
                                InterviewDate = c.InterviewDate,
                                InterviewResult = c.InterviewResult,
                                Status = c.Status,
                                OtpVerify = c.OtpVerify,
                                CompanyID = j.CompanyID,
                                Location = j.Location,
                                Department = j.Department,
                                JobType = j.JobType
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
                                "department" => query.Where(x => x.Department.Contains(value)),
                                "location" => query.Where(x => x.Location.Contains(value)),
                                "jobtype" => query.Where(x => x.JobType.Contains(value)),
                                "applicant" => long.TryParse(value, out var applicantId) ? query.Where(x => x.ApplicantID == applicantId) : query,
                                _ => query
                            };
                        }
                    }
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
                return await _context.Candidates.AsNoTracking().FirstOrDefaultAsync(x => x.CandidateID == id && x.IsDeleted == false);
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

        public async Task<bool> SaveOTPToDatabase(string Name, string Email, int CompanyID)
        {
            try
            {
                Random random = new Random();
                string otp = (random.Next(1000, 9999)).ToString();
                DateTime expirationDate = DateTime.Now.AddMinutes(10);

                var result = await _context.Database.ExecuteSqlRawAsync(@"IF EXISTS (SELECT 1 FROM OTPVerification WHERE Email = @Email)
                BEGIN
                    UPDATE OTPVerification
                    SET OTP = @OTP, ExpirationDate = @ExpirationDate
                    WHERE Email = @Email
                END
                ELSE
                BEGIN
                    INSERT INTO OTPVerification (Email, OTP, ExpirationDate)
                    VALUES (@Email, @OTP, @ExpirationDate)
                END", new SqlParameter("Email", Email), new SqlParameter("OTP", otp), new SqlParameter("ExpirationDate", expirationDate));
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

    <p>Untuk menyelesaikan proses verifikasi akun Anda di <strong>{(CompanyID == 2 ? "PT Trass Anugrah Makmur" : "PT Solusi Prima Packaging")}</strong>, masukkan One-Time Password (OTP) berikut:</p>

    <p><strong>OTP Anda:</strong> {otp}</p>

    <p>OTP ini hanya berlaku selama 10 menit. Jangan berbagi kode ini dengan orang lain.</p>

    <p>Jika Anda tidak melakukan permintaan ini, Anda dapat mengabaikan email ini.</p>

    <p>Terima kasih</p>

</body>
</html>
";

                    Utility.sendMail(Email, "", subject, body);

                    return result == 1;
                }
                catch (Exception ex)
                {
                }
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

        public async Task<bool> VerifyOTP(string email, string inputOtp)
        {
            try
            {
                string query = @"
                        SELECT CASE 
                                WHEN OTP = @InputOTP AND ExpirationDate > GETDATE()
                                THEN 1 
                                ELSE 0 
                               END Value
                        FROM OTPVerification
                        WHERE Email = @Email";

                var result = await _context.Database
                           .SqlQueryRaw<int>(query,
                                          new SqlParameter("@Email", email),
                                          new SqlParameter("@InputOTP", inputOtp))
                           .FirstOrDefaultAsync();
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
    }
}
