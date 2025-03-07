using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.src.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace sopra_hris_api.src.Services.API
{
    public class EmployeeShiftService : IServiceEmployeeShiftAsync<EmployeeShifts>
    {
        private readonly EFContext _context;

        public EmployeeShiftService(EFContext context)
        {
            _context = context;
        }

        public async Task<EmployeeShifts> CreateAsync(EmployeeShifts data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.EmployeeShifts.AddAsync(data);
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
                var obj = await _context.EmployeeShifts.FirstOrDefaultAsync(x => x.EmployeeShiftID == id && x.IsDeleted == false);
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

        public async Task<EmployeeShifts> EditAsync(EmployeeShifts data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.EmployeeShifts.FirstOrDefaultAsync(x => x.EmployeeShiftID == data.EmployeeShiftID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.ShiftID = data.ShiftID;
                obj.TransDate = data.TransDate;
                obj.GroupShiftID = data.GroupShiftID;

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


        public async Task<ListResponse<EmployeeShifts>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.EmployeeShifts where a.IsDeleted == false select a;

                // Searching
                //if (!string.IsNullOrEmpty(search))
                //    query = query.Where(x => x.Name.Contains(search)
                //        );

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
                                //"name" => query.Where(x => x.Name.Contains(value)),
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
                            //"name" => query.OrderByDescending(x => x.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            //"name" => query.OrderBy(x => x.Name),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.EmployeeShiftID);
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

                return new ListResponse<EmployeeShifts>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<EmployeeShifts> GetByIdAsync(long id)
        {
            try
            {
                return await _context.EmployeeShifts.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeShiftID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplateShift<EmployeeGroupShiftTemplate>> GetTemplateAsync(string filter)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                List<EmployeeGroupShiftTemplate> employeeGroupShiftTemplates = new List<EmployeeGroupShiftTemplate>();

                // Fetch shifts only once  
                var shifts = await _context.Shifts.Where(x => x.IsDeleted == false).ToListAsync();

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

                            if (fieldName == "type")
                            {
                                if (value == "employee")
                                {
                                    var employees = await _context.Employees
                                       .Where(e => e.IsDeleted == false && e.IsShift == true)
                                       .Join(_context.GroupShifts,
                                           e => e.GroupShiftID,
                                           gs => gs.GroupShiftID,
                                           (e, gs) => new EmployeeGroupShiftTemplate
                                           {
                                               EmployeeID = e.EmployeeID,
                                               Nik = e.Nik,
                                               Name = e.EmployeeName
                                           })
                                       .ToListAsync();

                                    employeeGroupShiftTemplates.AddRange(employees);
                                }
                                else if (value == "groupshift")
                                {
                                    var groupShifts = await _context.GroupShifts
                                        .Where(gs => gs.IsDeleted == false)
                                        .Select(gs => new EmployeeGroupShiftTemplate
                                        {
                                            GroupShiftID = gs.GroupShiftID,
                                            GroupShiftCode = gs.Code,
                                            GroupShiftName = gs.Name
                                        })
                                        .ToListAsync();

                                    employeeGroupShiftTemplates.AddRange(groupShifts);
                                }
                            }
                        }
                    }
                }

                return new ListResponseTemplateShift<EmployeeGroupShiftTemplate>(employeeGroupShiftTemplates, shifts);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error: {ex.Message}");
                if (ex.StackTrace != null)
                    Trace.WriteLine($"StackTrace: {ex.StackTrace}");

                throw;
            }
        }
        public async Task<ListResponseTemplate<EmployeeShifts>> SetEmployeeShiftsAsync(DataTable templates, bool isEmployeeBased, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                DataTable employeeShiftTypeTable = new DataTable();
                employeeShiftTypeTable.Columns.Add("EmployeeId", typeof(long));
                employeeShiftTypeTable.Columns.Add("ShiftCode", typeof(string));
                employeeShiftTypeTable.Columns.Add("TransDate", typeof(DateTime));
                employeeShiftTypeTable.Columns.Add("GroupShiftID", typeof(long));
                foreach (DataRow row in templates.Rows)
                {
                    if (isEmployeeBased && string.IsNullOrEmpty(row["EmployeeID"].ToString()))
                        continue;
                    if (!isEmployeeBased && string.IsNullOrEmpty(row["GroupShiftID"].ToString()))
                        continue;

                    long employeeId = isEmployeeBased ? Convert.ToInt64(row["EmployeeID"]) : 0L;
                    long groupShiftId = isEmployeeBased ? 0L : Convert.ToInt64(row["GroupShiftID"]);

                    for (int i = (isEmployeeBased ? 3 : 2); i < row.Table.Columns.Count; i++) 
                    {
                        string dateStr = row.Table.Columns[i].ColumnName;
                        string shiftCode = row[i].ToString();
                        if (string.IsNullOrEmpty(shiftCode))
                            continue;

                        if (DateTime.TryParse(dateStr, out DateTime transDate))
                            employeeShiftTypeTable.Rows.Add(employeeId, shiftCode, transDate, groupShiftId);                        
                    }
                }

                var templateParameter = new SqlParameter("@ShiftData", SqlDbType.Structured)
                {
                    TypeName = "dbo.EmployeeShiftType",
                    Value = employeeShiftTypeTable
                };
                var data = await _context.EmployeeShifts.FromSqlRaw(
                    $"EXEC usp_SaveEmployeeShifts @ShiftData = @ShiftData, @IsEmployeeBased = {isEmployeeBased}, @UserID = {UserID}",
                    templateParameter
                ).ToListAsync();

                await dbTrans.CommitAsync();
                //var data = await _context.EmployeeShifts.Where(x => x.IsDeleted == false).ToListAsync();
                return new ListResponseTemplate<EmployeeShifts>(data);
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
    }
}
