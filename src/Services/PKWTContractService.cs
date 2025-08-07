using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class PKWTContractService : IServiceAsync<PKWTContracts>
    {
        private readonly EFContext _context;

        public PKWTContractService(EFContext context)
        {
            _context = context;
        }

        public async Task<PKWTContracts> CreateAsync(PKWTContracts data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.PKWTContracts.AddAsync(data);
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
                var obj = await _context.PKWTContracts.FirstOrDefaultAsync(x => x.PWKTID == id && x.IsDeleted == false);
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

        public async Task<PKWTContracts> EditAsync(PKWTContracts data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.PKWTContracts.FirstOrDefaultAsync(x => x.PWKTID == data.PWKTID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.PKWTNo = data.PKWTNo;
                obj.StartDate = data.StartDate;
                obj.EndDate = data.EndDate;
                obj.LaidOffDate = data.LaidOffDate;
                obj.LaidOffEndDate = data.LaidOffEndDate;
                obj.ContractType = data.ContractType;
                obj.EmployeeID = data.EmployeeID;
                obj.Remarks = data.Remarks;
                obj.ContractsURL = data.ContractsURL;

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


        public async Task<ListResponse<PKWTContracts>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.PKWTContracts
                            join e in _context.Employees on a.EmployeeID equals e.EmployeeID
                            where a.IsDeleted == false
                            select new PKWTContracts
                            {
                                PWKTID = a.PWKTID,
                                EmployeeID = e.EmployeeID,
                                PKWTNo = a.PKWTNo,
                                StartDate = a.StartDate,
                                EndDate = a.EndDate,
                                ContractType = a.ContractType,
                                LaidOffDate = a.LaidOffDate,
                                LaidOffEndDate = a.LaidOffEndDate,
                                Remarks = a.Remarks,
                                EmployeeName = e.EmployeeName,
                                ContractsURL = a.ContractsURL
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.PKWTNo.Contains(search) || x.EmployeeName.Contains(search)
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
                                "pkwtno" => query.Where(x => x.PKWTNo.Contains(value)),
                                "employeeid" => query.Where(x => x.EmployeeID.Equals(value)),
                                "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                "type" => query.Where(x => x.ContractType.Contains(value)),
                                _ => query
                            };
                        }
                    }
                }

                DateTime dateNow = DateTime.Now;
                DateTime StartDate = new DateTime(dateNow.Year, dateNow.Month, 24);
                DateTime EndDate = new DateTime(dateNow.AddMonths(1).Year, dateNow.AddMonths(1).Month, 23);
                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                {
                    var dateRange = date.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    if (dateRange.Length == 2 && DateTime.TryParse(dateRange[0], out var startDate) && DateTime.TryParse(dateRange[1], out var endDate))
                    {
                        StartDate = startDate;
                        EndDate = endDate;
                    }
                    query = query.Where(x => x.EndDate.Date >= StartDate.Date && x.EndDate.Date <= EndDate.Date);
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
                            "pkwtno" => query.OrderByDescending(x => x.PKWTNo),
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            "type" => query.OrderByDescending(x => x.ContractType),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "pkwtno" => query.OrderBy(x => x.PKWTNo),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            "type" => query.OrderBy(x => x.ContractType),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.PWKTID);
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

                return new ListResponse<PKWTContracts>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<PKWTContracts> GetByIdAsync(long id)
        {
            try
            {
                return await _context.PKWTContracts.AsNoTracking().FirstOrDefaultAsync(x => x.PWKTID == id && x.IsDeleted == false);
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
