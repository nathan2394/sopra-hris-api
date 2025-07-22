using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class ApplicantFamilyService : IServiceAsync<ApplicantFamilys>
    {
        private readonly EFContext _context;

        public ApplicantFamilyService(EFContext context)
        {
            _context = context;
        }

        public async Task<ApplicantFamilys> CreateAsync(ApplicantFamilys data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.ApplicantFamilys.AddAsync(data);
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
                var obj = await _context.ApplicantFamilys.FirstOrDefaultAsync(x => x.FamilyID == id && x.IsDeleted == false);
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

        public async Task<ApplicantFamilys> EditAsync(ApplicantFamilys data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.ApplicantFamilys.FirstOrDefaultAsync(x => x.FamilyID == data.FamilyID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.ApplicantID = data.ApplicantID;
                obj.RelationshipType = data.RelationshipType;
                obj.FamilyName = data.FamilyName;
                obj.Gender = data.Gender;
                obj.FamilyDateOfBirth = data.FamilyDateOfBirth;
                obj.FamilyPlaceOfBirth = data.FamilyPlaceOfBirth;
                obj.ParentEducation = data.ParentEducation;
                obj.FamilyOccupation = data.FamilyOccupation;

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


        public async Task<ListResponse<ApplicantFamilys>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.ApplicantFamilys
                            where a.IsDeleted == false
                            select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.FamilyName.Contains(search)
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
                                "name" => query.Where(x => x.FamilyName.Contains(value)),
                                "applicant" => query.Where(x => x.ApplicantID.Equals(value)),
                                "relation" => query.Where(x => x.RelationshipType.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.FamilyName),
                            "relation" => query.OrderByDescending(x => x.RelationshipType),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.FamilyName),
                            "relation" => query.OrderBy(x => x.RelationshipType),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.FamilyID);
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

                return new ListResponse<ApplicantFamilys>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ApplicantFamilys> GetByIdAsync(long id)
        {
            try
            {
                return await _context.ApplicantFamilys.AsNoTracking().FirstOrDefaultAsync(x => x.FamilyID == id && x.IsDeleted == false);
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
