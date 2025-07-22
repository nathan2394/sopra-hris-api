using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class EducationHistoryService : IServiceAsync<EducationHistory>
    {
        private readonly EFContext _context;

        public EducationHistoryService(EFContext context)
        {
            _context = context;
        }

        public async Task<EducationHistory> CreateAsync(EducationHistory data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.EducationHistory.AddAsync(data);
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
                var obj = await _context.EducationHistory.FirstOrDefaultAsync(x => x.EducationID == id && x.IsDeleted == false);
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

        public async Task<EducationHistory> EditAsync(EducationHistory data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.EducationHistory.FirstOrDefaultAsync(x => x.EducationID == data.EducationID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.ApplicantID = data.ApplicantID;
                obj.EducationLevel = data.EducationLevel;
                obj.InstitutionName = data.InstitutionName;
                obj.InstitutionLocation = data.InstitutionLocation;
                obj.Major = data.Major;
                obj.EndYear = data.EndYear;
                obj.GPA = data.GPA;

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


        public async Task<ListResponse<EducationHistory>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.EducationHistory
                            where a.IsDeleted == false
                            select a;

                // Searching
                if (!string.IsNullOrEmpty(search)
                    //query = query.Where(x => x.FullName.Contains(search) || x.JobTitle.Contains(search)
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
                                "name" => query.Where(x => x.InstitutionName.Contains(value)),
                                "level" => query.Where(x => x.EducationLevel.Contains(value)),
                                "applicant" => query.Where(x => x.ApplicantID.Equals(value)),
                                "location" => query.Where(x => x.InstitutionLocation.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.InstitutionName),
                            "level" => query.OrderByDescending(x => x.EducationLevel),
                            "location" => query.OrderByDescending(x => x.InstitutionLocation),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.InstitutionName),
                            "level" => query.OrderBy(x => x.EducationLevel),
                            "location" => query.OrderBy(x => x.InstitutionLocation),
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

                return new ListResponse<EducationHistory>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<EducationHistory> GetByIdAsync(long id)
        {
            try
            {
                return await _context.EducationHistory.AsNoTracking().FirstOrDefaultAsync(x => x.EducationID == id && x.IsDeleted == false);
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
