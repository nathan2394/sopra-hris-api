using System;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class ApplicantService : IServiceApplicantAsync<Applicants>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicantService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<Applicants> CreateAsync(Applicants data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.Password = Utility.HashPassword(data.Password);
                data.ConsentSignedAt = null;
                await _context.Applicants.AddAsync(data);
                await _context.SaveChangesAsync();

                var candidatesToUpdate = await _context.Candidates.Where(c => c.Email == data.Email && c.IsDeleted == false && (c.ApplicantID == null || c.ApplicantID == 0)).ToListAsync();

                candidatesToUpdate.ForEach(x => x.ApplicantID = data.ApplicantID);

                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();

                data.Password = "";
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
                var obj = await _context.Applicants.FirstOrDefaultAsync(x => x.ApplicantID == id && x.IsDeleted == false);
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
        public async Task<Applicants> ChangePasswordAsync(ApplicantChangePassword data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Applicants.FirstOrDefaultAsync(x => x.ApplicantID == data.ApplicantID && x.IsDeleted == false);
                if (obj == null) return null;
                
                obj.Password = Utility.HashPassword(data.Password);
                obj.FullName = data.FullName;
                obj.MobilePhoneNumber = data.MobilePhoneNumber;
                
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
        public async Task<Applicants> EditAsync(Applicants data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Applicants.FirstOrDefaultAsync(x => x.ApplicantID == data.ApplicantID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.FullName = data.FullName;
                obj.Gender = data.Gender;
                obj.PlaceOfBirth = data.PlaceOfBirth;
                obj.DateOfBirth = data.DateOfBirth;
                obj.Religion = data.Religion;
                obj.MaritalStatus = data.MaritalStatus;
                obj.NoKTP = data.NoKTP;
                obj.NoSIM = data.NoSIM;
                obj.BloodType = data.BloodType;
                obj.HeightCM = data.HeightCM;
                obj.WeightKG = data.WeightKG;
                obj.Address = data.Address;
                obj.HomePhoneNumber = data.HomePhoneNumber;
                obj.MobilePhoneNumber = data.MobilePhoneNumber;
                obj.ConsentSignedAt = data.ConsentSignedAt;
                obj.ResumeURL = data.ResumeURL;
                obj.HasWorkExperience = data.HasWorkExperience;

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
        public async Task<string> SendForgotPasswordOTPAsync(string email)
        {
            try
            {
                // Check if user exists
                var checkQuery = "SELECT TOP 1 FullName FROM Applicants WHERE Email = @Email AND IsDeleted = 0";
                string name = null;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = checkQuery;
                    command.Parameters.Add(new SqlParameter("@Email", email));
                    _context.Database.OpenConnection();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            name = reader.IsDBNull(0) ? null : reader.GetString(0);
                        }
                    }
                }

                if (string.IsNullOrEmpty(name))
                    return "Account not found.";

                // Optional: rate limit (same as before)
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
                    command.Parameters.Add(new SqlParameter("@Email", email));

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

                if (lastRequestTime != null && lastRequestTime > DateTime.Now.AddHours(-1) && requestCount >= 5)
                    return "Too many OTP requests in the last hour.";

                // Generate OTP
                string otp = new Random().Next(1000, 9999).ToString();
                DateTime expiration = DateTime.Now.AddMinutes(10);

                var otpQuery = @"
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

                await _context.Database.ExecuteSqlRawAsync(otpQuery,
                    new SqlParameter("@Email", email),
                    new SqlParameter("@OTP", otp),
                    new SqlParameter("@ExpirationDate", expiration));

                // Send email
                string subject = "Reset Password";
                string body = $@"<!DOCTYPE html>
<html lang=""id"">
<head>
    <meta charset=""UTF-8"">
    <title>Reset Kata Sandi</title>
</head>
<body>
    <p>Halo {name},</p>
    <p>Kami menerima permintaan untuk mereset kata sandi akun Anda. Gunakan kode OTP berikut untuk melanjutkan proses reset:</p>
    <h2>{otp}</h2>
    <p>OTP ini berlaku selama <strong>10 menit</strong>. Jangan berikan kode ini kepada siapa pun.</p>
    <p>Jika Anda tidak merasa melakukan permintaan ini, silakan abaikan email ini.</p>
    <p>Terima kasih</p>
</body>
</html>";

                Utility.sendMail(email, "", subject, body);

                return "OTP sent successfully.";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return "An error occurred.";
            }
        }
        public async Task<ListResponse<Applicants>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Applicants
                            where a.IsDeleted == false
                            select new Applicants
                            {
                                ApplicantID = a.ApplicantID,
                                Password = "",
                                FullName = a.FullName,
                                Gender = a.Gender,
                                Address = a.Address,
                                BloodType = a.BloodType,
                                DateOfBirth = a.DateOfBirth,
                                HeightCM = a.HeightCM,
                                Email = a.Email,
                                HomePhoneNumber = a.HomePhoneNumber,
                                MobilePhoneNumber = a.MobilePhoneNumber,
                                Religion = a.Religion,
                                PlaceOfBirth = a.PlaceOfBirth,
                                MaritalStatus = a.MaritalStatus,
                                NoKTP = a.NoKTP,
                                NoSIM = a.NoSIM,
                                WeightKG = a.WeightKG,
                                ConsentSignedAt = a.ConsentSignedAt,
                                ProfileCompletion = a.ProfileCompletion,
                                ResumeURL = a.ResumeURL,
                                HasWorkExperience = a.HasWorkExperience
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.FullName.Contains(search) || x.Email.Contains(search) || x.MobilePhoneNumber.Contains(search)
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
                                "name" => query.Where(x => x.FullName.Contains(value)),
                                "phoneno" => query.Where(x => x.MobilePhoneNumber.Contains(value)),
                                "email" => query.Where(x => x.Email.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.FullName),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.FullName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.ApplicantID);
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

                return new ListResponse<Applicants>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Applicants> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Applicants.Where(x => x.ApplicantID == id && x.IsDeleted == false).Select(a => new Applicants
                {
                    ApplicantID = a.ApplicantID,
                    Password = "",
                    FullName = a.FullName,
                    Gender = a.Gender,
                    Address = a.Address,
                    BloodType = a.BloodType,
                    DateOfBirth = a.DateOfBirth,
                    HeightCM = a.HeightCM,
                    Email = a.Email,
                    HomePhoneNumber = a.HomePhoneNumber,
                    MobilePhoneNumber = a.MobilePhoneNumber,
                    Religion = a.Religion,
                    PlaceOfBirth = a.PlaceOfBirth,
                    MaritalStatus = a.MaritalStatus,
                    NoKTP = a.NoKTP,
                    NoSIM = a.NoSIM,
                    WeightKG = a.WeightKG,
                    ConsentSignedAt = a.ConsentSignedAt,
                    ProfileCompletion = a.ProfileCompletion,
                    ResumeURL = a.ResumeURL,
                    HasWorkExperience = a.HasWorkExperience
                }).AsNoTracking().FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<int> GetCompletionAsync(long id)
        {
            try
            {
                var sql = "SELECT ISNULL(ProfileCompletion, 0) Value FROM Applicants WHERE ApplicantID = @ApplicantID";
                var percentage = await _context.Database.SqlQueryRaw<int>(sql, new SqlParameter("@ApplicantID", id))
                                                         .FirstOrDefaultAsync();
                return percentage;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<bool> ProfileCompletion(long id, int Completion)
        {
            try
            {
                var sql = @"Update Applicants SET ProfileCompletion=@ProfileCompletion WHERE ApplicantID=@ApplicantID";
                var parameters = new[]
                {
            new SqlParameter("@ApplicantID", id),
            new SqlParameter("@ProfileCompletion", Completion)
        };

                await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, parameters);

                if (rowsAffected > 0)
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<string> VerifyOTPResetAsync(VerifyResetPasswordRequest request)
        {
            try
            {
                // Step 1: Verify OTP
                var otpQuery = @"
            SELECT TOP 1 ExpirationDate
            FROM OTPVerification
            WHERE Email = @Email AND OTP = @OTP AND IsVerify = 0";

                DateTime? expirationDate = null;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = otpQuery;
                    command.Parameters.Add(new SqlParameter("@Email", request.Email));
                    command.Parameters.Add(new SqlParameter("@OTP", request.OTP));
                    _context.Database.OpenConnection();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            expirationDate = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                        }
                    }
                }

                if (expirationDate == null)
                    return "Invalid OTP.";
                if (expirationDate < DateTime.Now)
                    return "OTP has expired.";

                string otp = new Random().Next(1000, 9999).ToString();
                DateTime expiration = DateTime.Now.AddMinutes(10);

                // Step 2: Mark OTP as used
                var updateOTP = @"
                    UPDATE OTPVerification
                    SET IsVerify = 1
                    WHERE Email = @Email AND OTP = @OTP AND IsVerify=0

                    INSERT INTO OTPVerification (Email, OTP, ExpirationDate, IsVerify, RequestCount, LastRequestTime)
                    VALUES (@Email, @NewOTP, @ExpirationDate, 0, 1, GETDATE())";

                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(updateOTP,
                    new SqlParameter("@Email", request.Email),
                    new SqlParameter("@OTP", request.OTP),
                    new SqlParameter("@NewOTP", otp),
                    new SqlParameter("@ExpirationDate", expiration));

                return rowsAffected > 0 ? $"Code: {otp}" : "Failed to reset password.";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);
                return "An error occurred.";
            }
        }
        public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var otpQuery = @"
            SELECT TOP 1 ExpirationDate
            FROM OTPVerification
            WHERE Email = @Email AND OTP = @OTP AND IsVerify = 0 ORDER BY ExpirationDate DESC";

                DateTime? expirationDate = null;

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = otpQuery;
                    command.Parameters.Add(new SqlParameter("@Email", request.Email));
                    command.Parameters.Add(new SqlParameter("@OTP", request.OTP));
                    _context.Database.OpenConnection();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            expirationDate = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                        }
                    }
                }

                if (expirationDate == null)
                    return "Invalid OTP.";
                if (expirationDate < DateTime.Now)
                    return "OTP has expired.";
                var passwordHash = Utility.HashPassword(request.NewPassword);

                var updateOTP = @"
                    UPDATE OTPVerification
                    SET IsVerify = 1
                    WHERE Email = @Email AND OTP = @OTP AND IsVerify=0";

                await _context.Database.ExecuteSqlRawAsync(updateOTP,
                    new SqlParameter("@Email", request.Email),
                    new SqlParameter("@OTP", request.OTP));

                var updatePassword = @"
            UPDATE Applicants
            SET Password = @Password
            WHERE Email = @Email AND IsDeleted = 0";

                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(updatePassword,
                    new SqlParameter("@Password", passwordHash),
                    new SqlParameter("@Email", request.Email));

                return rowsAffected > 0 ? "Password has been reset successfully." : "Failed to reset password.";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);
                return "An error occurred.";
            }
        }
    }
}
