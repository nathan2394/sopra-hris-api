using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class SupervisorBenefitService : IServiceAsync<SupervisorBenefit>
    {
        private readonly EFContext _context;

        public SupervisorBenefitService(EFContext context)
        {
            _context = context;
        }

        public async Task<SupervisorBenefit> CreateAsync(SupervisorBenefit data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SupervisorBenefit.AddAsync(data);
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
                var obj = await _context.SupervisorBenefit.FirstOrDefaultAsync(x => x.SupervisorBenefitID == id && x.IsDeleted == false);
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

        public async Task<SupervisorBenefit> EditAsync(SupervisorBenefit data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.SupervisorBenefit.FirstOrDefaultAsync(x => x.SupervisorBenefitID == data.SupervisorBenefitID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.RewardMonth = data.RewardMonth;
                obj.RewardYear = data.RewardYear;
                obj.TotalOvertimeHours = data.TotalOvertimeHours;

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


        public async Task<ListResponse<SupervisorBenefit>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.SupervisorBenefit
                            join e in _context.Employees on a.EmployeeID equals e.EmployeeID
                            where a.IsDeleted == false
                            select new SupervisorBenefit
                            {
                                EmployeeID = e.EmployeeID,
                                EmployeeName = e.EmployeeName,
                                RewardMonth = a.RewardMonth,
                                RewardYear = a.RewardYear,
                                TotalOvertimeHours = a.TotalOvertimeHours,
                                SupervisorBenefitID = a.SupervisorBenefitID
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
                                "month" => query.Where(x => x.RewardMonth.Equals(value)),
                                "year" => query.Where(x => x.RewardYear.Equals(value)),
                                "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                "employeeid" => query.Where(x => x.EmployeeID.Equals(value)),
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
                    query = query.OrderByDescending(x => x.SupervisorBenefitID);
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

                return new ListResponse<SupervisorBenefit>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<SupervisorBenefit> GetByIdAsync(long id)
        {
            try
            {
                return await _context.SupervisorBenefit.AsNoTracking().FirstOrDefaultAsync(x => x.SupervisorBenefitID == id && x.IsDeleted == false);
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
