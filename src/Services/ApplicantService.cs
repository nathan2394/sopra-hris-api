using System;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class ApplicantService : IServiceAsync<Applicants>
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
                string company = "";
                string jobtitle = "";
                string password = "";
                var candidates = await _context.Candidates.FirstOrDefaultAsync(x => x.IsDeleted == false && x.CandidateID == data.CandidateID);
                if (candidates != null)
                {
                    var jobs = await _context.Jobs.FirstOrDefaultAsync(x => x.JobID == candidates.JobID);
                    if (jobs != null)
                    {
                        company = jobs.CompanyID == 2 ? "PT Trass Anugrah Makmur" : "PT Solusi Prima Packaging";
                        jobtitle = jobs.JobTitle;
                    }
                }
                var check_Applicant = await _context.Applicants.FirstOrDefaultAsync(x => x.IsDeleted == false && x.Email == data.Email);
                if (check_Applicant == null)
                {
                    password = string.Concat(data.Email.Length >= 4 ? data.Email.Substring(0, 4) : data.Email, data.DateIn.Value.ToString("HHmmss"));
                    data.Password = Utility.HashPassword(password);
                    data.ConsentSignedAt = null;

                    await _context.Applicants.AddAsync(data);
                    await _context.SaveChangesAsync();


                    await dbTrans.CommitAsync();

                    try
                    {

                        string subject = $"Tindak Lanjut Lamaran Anda: Pengisian Biodata untuk Proses Seleksi";
                        string body = $@"
                    <!DOCTYPE html>
<html lang=""id"">
<head>
    <meta charset=""UTF-8"">
    <title>Informasi Akun dan Biodata</title>
</head>
<body>
    <p>Dear, {data.FullName},</p>

    <p>Terima kasih telah melamar untuk posisi <strong>{jobtitle}</strong> di <strong>{company}</strong>. Kami senang menginformasikan bahwa Anda telah lolos ke tahap berikutnya dalam proses seleksi.</p>

    <p>Untuk melanjutkan, kami mohon Anda untuk mengisi biodata melalui link berikut. Kami juga telah membuatkan akun untuk Anda.</p>

    <p><strong>Detail Akun:</strong></p>
    <ul>
        <li>Username: {data.Email}</li>
        <li>Password: {password}</li>
    </ul>

    <p><strong>Link Biodata:</strong><a href=""https://portal.solusi-pack.com/"">click here</a></p>

    <p>Jika Anda membutuhkan bantuan atau memiliki pertanyaan, jangan ragu untuk menghubungi kami.</p>

    <p>Kami menantikan data biodata Anda dan proses selanjutnya.</p>

    <p>Terima kasih</p>
</body>
</html>";
                        Utility.sendMail(String.Join(";", data.Email), "", subject, body);

                    }
                    catch (Exception ex) { }
                }
                else
                    data = check_Applicant;

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
    }
}
