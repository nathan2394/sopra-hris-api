using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sopra_hris_api.src.Services.API
{
    public class EmployeeShiftService : IServiceEmployeeShiftAsync<EmployeeShifts>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeShiftService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

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
                var employeeid = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var roleid = Convert.ToInt64(User.FindFirstValue("roleid"));
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.EmployeeShifts
                            join s in _context.Shifts on a.ShiftID equals s.ShiftID
                            join e in _context.Employees on a.EmployeeID equals e.EmployeeID
                            where a.IsDeleted == false && ((a.EmployeeID == employeeid && (roleid == 2 || roleid == 5)) || (roleid != 2 && roleid != 5))
                            select new EmployeeShifts
                            {
                                EmployeeShiftID = a.EmployeeShiftID,
                                EmployeeID = a.EmployeeID,
                                ShiftID = a.ShiftID,
                                TransDate = a.TransDate,
                                GroupShiftID = a.GroupShiftID,
                                EmployeeName = e.EmployeeName,
                                ShiftCode = s.Code,
                                ShiftName = s.Name
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeeName.Contains(search) || x.ShiftCode.Contains(search) || x.ShiftName.Contains(search)
                        );

                if (!string.IsNullOrEmpty(date))
                {
                    DateTime queryDate = DateTime.Parse(date);
                    query = query.Where(x => x.TransDate.Value.Date == queryDate.Date);
                }
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
                                "shiftcode" => query.Where(x => x.ShiftCode.Contains(value)),
                                "shift" => query.Where(x => x.ShiftName.Contains(value)),
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

                DateTime queryDate = new DateTime(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month, 23);

                IQueryable<Employees> employeeQuery = _context.Employees.Where(e => e.IsDeleted == false && e.IsShift == true && (!e.EndWorkingDate.HasValue || e.EndWorkingDate.Value.Date >= queryDate.Date));                
                string typeFilter = null;

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
                                typeFilter = value.ToLower();
                            else
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();

                                switch (fieldName)
                                {
                                    case "group":
                                        employeeQuery = employeeQuery.Where(e => Ids.Contains(e.GroupID));
                                        break;
                                    case "department":
                                        employeeQuery = employeeQuery.Where(e => Ids.Contains(e.DepartmentID ?? 0));
                                        break;
                                    case "function":
                                        employeeQuery = employeeQuery.Where(e => Ids.Contains(e.FunctionID ?? 0));
                                        break;
                                    case "employeetype":
                                        employeeQuery = employeeQuery.Where(e => Ids.Contains(e.EmployeeTypeID));
                                        break;
                                    case "division":
                                        employeeQuery = employeeQuery.Where(e => Ids.Contains(e.DivisionID ?? 0));
                                        break;
                                }
                            }
                        }
                    }
                }

                if (typeFilter == "employee")
                {
                    var employees = await employeeQuery
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
                else if (typeFilter == "groupshift")
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
        public async Task<ListResponseTemplate<EmployeeShiftsDTO>> SetEmployeeShiftsAsync(DataTable templates, bool isEmployeeBased, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Database.SetCommandTimeout(300);
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
                await _context.Database.ExecuteSqlRawAsync(
                    $"EXEC usp_SaveEmployeeShifts @ShiftData = @ShiftData, @IsEmployeeBased = {isEmployeeBased}, @UserID = {UserID}",
                    templateParameter);

                var data = await _context.EmployeeShiftsDTO.FromSqlRaw(
                    @$"SELECT es.EmployeeShiftID,es.EmployeeID,es.ShiftID,es.TransDate,es.GroupShiftID,
		                    c.Code ShiftCode, c.Name ShiftName, b2.Code GroupShiftCode, b2.Name GroupShiftName, b.EmployeeName
                        FROM EmployeeShifts es 
	                    INNER JOIN @ShiftData a on ((a.EmployeeId=es.EmployeeID or es.GroupShiftID=a.GroupShiftID)) and a.TransDate=es.TransDate
                        LEFT JOIN Employees b ON es.EmployeeID = b.EmployeeID  
                        LEFT JOIN GroupShifts b2 ON es.GroupShiftID = b2.GroupShiftID  
                        LEFT JOIN Shifts c ON es.ShiftID = c.ShiftID  
                        WHERE es.Isdeleted=0", templateParameter).ToListAsync();

                await dbTrans.CommitAsync();

                return new ListResponseTemplate<EmployeeShiftsDTO>(data);
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
