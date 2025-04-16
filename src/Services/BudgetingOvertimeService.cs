using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class BudgetingOvertimeService : IServiceAsync<BudgetingOvertimes>
    {
        private readonly EFContext _context;

        public BudgetingOvertimeService(EFContext context)
        {
            _context = context;
        }

        public async Task<BudgetingOvertimes> CreateAsync(BudgetingOvertimes data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.BudgetingOvertimes.AddAsync(data);
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
                var obj = await _context.BudgetingOvertimes.FirstOrDefaultAsync(x => x.BudgetingOvertimesID == id && x.IsDeleted == false);
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

        public async Task<BudgetingOvertimes> EditAsync(BudgetingOvertimes data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.BudgetingOvertimes.FirstOrDefaultAsync(x => x.BudgetingOvertimesID == data.BudgetingOvertimesID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.BudgetMonth = data.BudgetMonth;
                obj.BudgetYear = data.BudgetYear;
                obj.TotalOvertimeHours= data.TotalOvertimeHours;
                obj.TotalOvertimeAmount = data.TotalOvertimeAmount;
                obj.DepartmentID = data.DepartmentID;

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


        public async Task<ListResponse<BudgetingOvertimes>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from b in _context.BudgetingOvertimes
                            join d in _context.Departments on b.DepartmentID equals d.DepartmentID
                            where b.IsDeleted == false
                            select new BudgetingOvertimes
                            {
                                BudgetingOvertimesID = b.BudgetingOvertimesID,
                                BudgetMonth = b.BudgetMonth,
                                BudgetYear = b.BudgetYear,
                                TotalOvertimeAmount = b.TotalOvertimeAmount,
                                TotalOvertimeHours = b.TotalOvertimeHours,
                                DepartmentID = b.DepartmentID,
                                DepartmentName = d.Name,
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.DepartmentName.Contains(search)
                        );

                int month = DateTime.Now.Month;
                int year = DateTime.Now.Year;
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

                            if (fieldName == "month")
                                Int32.TryParse(value, out month);
                            else if (fieldName == "year")
                                Int32.TryParse(value, out year);

                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.DepartmentName.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.DepartmentName),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.DepartmentName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.BudgetingOvertimesID);
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

                return new ListResponse<BudgetingOvertimes>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<BudgetingOvertimes> GetByIdAsync(long id)
        {
            try
            {
                return await _context.BudgetingOvertimes.AsNoTracking().FirstOrDefaultAsync(x => x.BudgetingOvertimesID == id && x.IsDeleted == false);
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
