using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class WarningLetterService : IServiceAsync<WarningLetters>
    {
        private readonly EFContext _context;

        public WarningLetterService(EFContext context)
        {
            _context = context;
        }

        public async Task<WarningLetters> CreateAsync(WarningLetters data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.WarningLetters.AddAsync(data);
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
                var obj = await _context.WarningLetters.FirstOrDefaultAsync(x => x.WarningLetterID == id && x.IsDeleted == false);
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

        public async Task<WarningLetters> EditAsync(WarningLetters data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.WarningLetters.FirstOrDefaultAsync(x => x.WarningLetterID == data.WarningLetterID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.WarningDate = data.WarningDate;
                obj.WarningType = data.WarningType;
                obj.Reason = data.Reason;
                obj.IssuedBy = data.IssuedBy;
                obj.AcknowledgementDate = data.AcknowledgementDate;
                obj.Remarks = data.Remarks;

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


        public async Task<ListResponse<WarningLetters>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.WarningLetters
                            join e in _context.Employees on a.EmployeeID equals e.EmployeeID
                            where a.IsDeleted == false
                            select new WarningLetters
                            {
                                EmployeeID = e.EmployeeID,
                                AcknowledgementDate = a.AcknowledgementDate,
                                EmployeeName = e.EmployeeName,
                                IssuedBy = a.IssuedBy,
                                Reason = a.Reason,
                                Remarks = a.Remarks,
                                WarningDate = a.WarningDate,
                                WarningType = a.WarningType,
                                WarningLetterID = a.WarningLetterID,
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeeName.Contains(search)
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
                                "name" => query.Where(x => x.EmployeeName.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.EmployeeName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.WarningLetterID);
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

                return new ListResponse<WarningLetters>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<WarningLetters> GetByIdAsync(long id)
        {
            try
            {
                return await _context.WarningLetters.AsNoTracking().FirstOrDefaultAsync(x => x.WarningLetterID == id && x.IsDeleted == false);
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
