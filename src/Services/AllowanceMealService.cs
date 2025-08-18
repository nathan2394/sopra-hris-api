using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace sopra_hris_api.src.Services.API
{
    public class AllowanceMealService : IServiceUploadAsync<AllowanceMeals>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AllowanceMealService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<AllowanceMeals> CreateAsync(AllowanceMeals data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.AllowanceMeals.AddAsync(data);
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
                var obj = await _context.AllowanceMeals.FirstOrDefaultAsync(x => x.AllowanceMealID == id && x.IsDeleted == false);
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

        public async Task<AllowanceMeals> EditAsync(AllowanceMeals data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.AllowanceMeals.FirstOrDefaultAsync(x => x.AllowanceMealID == data.AllowanceMealID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.TransDate = data.TransDate;

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


        public async Task<ListResponse<AllowanceMeals>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = (from a in _context.AllowanceMeals
                             join e in _context.Employees on a.EmployeeID equals e.EmployeeID
                             join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                             from d in deptGroup.DefaultIfEmpty()
                             join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                             from di in divGroup.DefaultIfEmpty()
                             where a.IsDeleted == false && e.GroupID != 15
                             select new AllowanceMeals
                             {
                                 AllowanceMealID = a.AllowanceMealID,
                                 EmployeeID = a.EmployeeID,
                                 TransDate = a.TransDate,
                                 DepartmentID = d.DepartmentID,
                                 DivisionID = di.DivisionID,
                                 DepartmentName = d.Name,
                                 DivisionName = di != null ? di.Name : null,
                                 EmployeeName = e.EmployeeName,
                                 NIK = e.Nik
                             });
                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.DivisionName.Contains(search)
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
                            if (fieldName == "department" || fieldName == "division" )
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                            }
                            else
                                query = fieldName switch
                                {
                                    "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                    "employeeid" => query.Where(x => x.EmployeeID.Equals(value)),
                                    _ => query
                                };
                        }
                    }
                }
                DateTime queryDate = DateTime.Now;
                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                {
                    DateTime.TryParse(date, out queryDate);
                    query = query.Where(x => x.TransDate.Date == queryDate.Date);
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
                            "department" => query.OrderByDescending(x => x.DepartmentName),
                            "division" => query.OrderByDescending(x => x.DivisionName),
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "department" => query.OrderBy(x => x.DepartmentName),
                            "division" => query.OrderBy(x => x.DivisionName),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.AllowanceMealID);
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

                return new ListResponse<AllowanceMeals>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<AllowanceMeals> GetByIdAsync(long id)
        {
            try
            {
                return await _context.AllowanceMeals.AsNoTracking().FirstOrDefaultAsync(x => x.AllowanceMealID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<AllowanceMealDTO>> GetTemplate(string search, string sort, string filter, string date)
        {
            try
            {
                DateTime queryDate = DateTime.Now;
                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                    DateTime.TryParse(date, out queryDate);
                
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = (from e in _context.Employees
                             join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                             from d in deptGroup.DefaultIfEmpty()
                             join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                             from di in divGroup.DefaultIfEmpty()
                             where e.IsDeleted == false && e.GroupID != 15 && (!e.EndWorkingDate.HasValue || e.EndWorkingDate.Value.Date >= queryDate.Date)
                             select new AllowanceMealDTO
                             {
                                 EmployeeID = e.EmployeeID,
                                 TransDate = queryDate.Date,
                                 EmployeeName = e.EmployeeName,
                                 NIK = e.Nik,
                                 DepartmentID = d.DepartmentID,
                                 DepartmentName = d.Name,
                                 DivisionID = di.DivisionID,
                                 DivisionName = di != null ? di.Name : null,
                             });
                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.DivisionName.Contains(search)
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
                            if (fieldName == "department" || fieldName == "division")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                            }
                            else
                                query = fieldName switch
                                {
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
                            "department" => query.OrderByDescending(x => x.DepartmentName),
                            "division" => query.OrderByDescending(x => x.DivisionName),
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "department" => query.OrderBy(x => x.DepartmentName),
                            "division" => query.OrderBy(x => x.DivisionName),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.EmployeeID);
                }
                
                // Get Data
                var data = await query.ToListAsync();

                return new ListResponseTemplate<AllowanceMealDTO>(data);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<AllowanceMeals>> UploadAsync(DataTable data, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                DateTime filterDate = DateTime.Now;
                DataTable dataTypeTable = new DataTable();
                dataTypeTable.Columns.Add("EmployeeId", typeof(long));                
                dataTypeTable.Columns.Add("TransDate", typeof(DateTime));
                dataTypeTable.Columns.Add("Meal", typeof(int));
                foreach (DataRow row in data.Rows)
                {
                    long employeeId = Convert.ToInt64(row["EmployeeID"]);
                    DateTime transDate = Convert.ToDateTime(row["TransDate"]);
                    filterDate = transDate;
                    int meal = Convert.ToInt32(row["Meal"]);
                    dataTypeTable.Rows.Add(employeeId, transDate, meal);
                }

                var templateParameter = new SqlParameter("@MealData", SqlDbType.Structured)
                {
                    TypeName = "dbo.EmployeeMealType",
                    Value = dataTypeTable
                };
                await _context.Database.ExecuteSqlRawAsync(
                    $"EXEC usp_SaveEmployeeMeals @MealData = @MealData, @UserID = {UserID}",
                    templateParameter);

                var result = await (from a in _context.AllowanceMeals
                                    join e in _context.Employees on a.EmployeeID equals e.EmployeeID
                                    join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                                    from d in deptGroup.DefaultIfEmpty()
                                    join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                                    from di in divGroup.DefaultIfEmpty()
                                    where a.IsDeleted == false
                                     && a.TransDate.Date == filterDate.Date
                                    select new AllowanceMeals
                                    {
                                        AllowanceMealID = a.AllowanceMealID,
                                        EmployeeID = a.EmployeeID,
                                        TransDate = a.TransDate,
                                        DepartmentID = d.DepartmentID,
                                        DivisionID = di.DivisionID,
                                        DepartmentName = d.Name,
                                        DivisionName = di != null ? di.Name : null,
                                        EmployeeName = e.EmployeeName,
                                        NIK = e.Nik
                                    }).AsNoTracking().ToListAsync();

                await dbTrans.CommitAsync();
                return new ListResponseTemplate<AllowanceMeals>(result);
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
