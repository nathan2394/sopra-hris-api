using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sopra_hris_api.src.Services.API
{
    public class JobService : IServiceJobsAsync<Jobs>
    {
        private readonly EFContext _context;

        public JobService(EFContext context)
        {
            _context = context;
        }

        public async Task<Jobs> CreateAsync(Jobs data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Jobs.AddAsync(data);
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
                var obj = await _context.Jobs.FirstOrDefaultAsync(x => x.JobID == id && x.IsDeleted == false);
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

        public async Task<Jobs> EditAsync(Jobs data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Jobs.FirstOrDefaultAsync(x => x.JobID == data.JobID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.CompanyID = data.CompanyID;
                obj.Department = data.Department;
                obj.JobTitle = data.JobTitle;
                obj.JobDescription = data.JobDescription;
                obj.JobType = data.JobType;
                obj.Location = data.Location;
                obj.SalaryMin = data.SalaryMin;
                obj.SalaryMax = data.SalaryMax;
                obj.Tags = data.Tags;
                obj.IsActive = data.IsActive;
                obj.PublicationDate = data.PublicationDate;
                obj.ExpirationDate = data.ExpirationDate;

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

        public async Task<ListResponse<Jobs>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Jobs where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.JobTitle.Contains(search) || x.JobDescription.Contains(search)
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
                                "title" => query.Where(x => x.JobTitle.Contains(value)),
                                "type" => query.Where(x => x.JobType.Contains(value)),
                                "location" => query.Where(x => x.Location.Contains(value)),
                                "department" => query.Where(x => x.Department.Contains(value)),
                                "tags" => query.Where(x => x.Tags.Contains(value)),
                                "company" => query.Where(x => x.CompanyID.Equals(value)),
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
                            "publish" => query.OrderByDescending(x => x.PublicationDate),
                            "expiry" => query.OrderByDescending(x => x.ExpirationDate),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "publish" => query.OrderBy(x => x.PublicationDate),
                            "expiry" => query.OrderBy(x => x.ExpirationDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.JobID);
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

                return new ListResponse<Jobs>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Jobs> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(x => x.JobID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetFilters(string filter)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                if (string.IsNullOrEmpty(filter))
                {
                    return new Dictionary<string, object>();
                }

                var filterParts = filter.Split(':');
                if (filterParts.Length != 2 || filterParts[0].Trim().ToLower() != "type")
                {
                    // Jika format tidak sesuai, kembalikan daftar kosong
                    return new Dictionary<string, object>();
                }
                var filterType = filterParts[1].Trim().ToLower();

                IQueryable<string> query;

                // Tentukan query berdasarkan tipe filter
                switch (filterType)
                {
                    case "jobtype":
                        query = _context.Jobs
                                        .Where(j => j.IsDeleted == false)
                                        .Select(j => j.JobType)
                                        .Distinct();
                        break;
                    case "department":
                        query = _context.Jobs
                                        .Where(j => j.IsDeleted == false)
                                        .Select(j => j.Department)
                                        .Distinct();
                        break;
                    case "location":
                        query = _context.Jobs
                                        .Where(j => j.IsDeleted == false)
                                        .Select(j => j.Location)
                                        .Distinct();
                        break;
                    default:
                        // Jika tipe filter tidak sesuai, kembalikan daftar kosong
                        return new Dictionary<string, object>();
                }

                // Get Data
                var data = await query.ToListAsync();

                var result = new Dictionary<string, object>
                {
                    { filterType, data }
                };

                return result;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<bool> SaveOTPToDatabase(string Name, string Email, int CompanyID)
        {
            throw new NotImplementedException();
        }

        public Task<bool> VerifyOTP(string email, string inputOtp)
        {
            throw new NotImplementedException();
        }
    }
}
