using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class OtherReferenceService : IServiceAsync<OtherReferences>
    {
        private readonly EFContext _context;

        public OtherReferenceService(EFContext context)
        {
            _context = context;
        }

        public async Task<OtherReferences> CreateAsync(OtherReferences data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.OtherReferences.AddAsync(data);
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
                var obj = await _context.OtherReferences.FirstOrDefaultAsync(x => x.OtherReferenceID == id && x.IsDeleted == false);
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

        public async Task<OtherReferences> EditAsync(OtherReferences data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.OtherReferences.FirstOrDefaultAsync(x => x.OtherReferenceID == data.OtherReferenceID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.ApplicantID = data.ApplicantID;
                obj.ReferenceFullName = data.ReferenceFullName;
                obj.ReferencePosition = data.ReferencePosition;
                obj.Relationship = data.Relationship;
                obj.ReferenceCompanyName = data.ReferenceCompanyName;
                obj.ReferencePhoneNumber = data.ReferencePhoneNumber;
                obj.ReferenceAddress = data.ReferenceAddress;

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


        public async Task<ListResponse<OtherReferences>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.OtherReferences
                            where a.IsDeleted == false
                            select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.ReferenceFullName.Contains(search) || x.ReferencePosition.Contains(search)
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
                                "name" => query.Where(x => x.ReferenceFullName.Contains(value)),
                                "position" => query.Where(x => x.ReferencePosition.Contains(value)),
                                "relation" => query.Where(x => x.Relationship.Contains(value)),
                                "applicant" => query.Where(x => x.ApplicantID.Equals(value)),
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
                            "name" => query.OrderByDescending(x => x.ReferenceFullName),
                            "position" => query.OrderByDescending(x => x.ReferencePosition),
                            "relation" => query.OrderByDescending(x => x.Relationship),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.ReferenceFullName),
                            "position" => query.OrderBy(x => x.ReferencePosition),
                            "relation" => query.OrderBy(x => x.Relationship),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.OtherReferenceID);
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

                return new ListResponse<OtherReferences>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<OtherReferences> GetByIdAsync(long id)
        {
            try
            {
                return await _context.OtherReferences.AsNoTracking().FirstOrDefaultAsync(x => x.OtherReferenceID == id && x.IsDeleted == false);
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
