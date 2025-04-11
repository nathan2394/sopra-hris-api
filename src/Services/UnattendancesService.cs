using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using sopra_hris_api.src.Entities;

namespace sopra_hris_api.src.Services.API
{
    public class UnattendanceService : IServiceUnattendanceOVTAsync<Unattendances>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UnattendanceService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;
        
        private async Task<int> CalculateEffectiveDuration(DateTime startDate, DateTime endDate, long employeeId)
        {
            var totalDays = 0;
            var employee = await (from e in _context.Employees
                join s in _context.Shifts on e.ShiftID equals s.ShiftID
                                  where e.EmployeeID == employeeId && e.IsDeleted == false
                                  select new
                                  {
                                      e.IsShift,
                                      s.WorkingDays
                                  }).FirstOrDefaultAsync();
            if (employee == null)
                return 0;

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var isHoliday = await _context.Holidays.AnyAsync(h => h.TransDate == date && h.IsDeleted == false);
                var isNonWorkingShift = await _context.EmployeeShifts.AnyAsync(s => s.EmployeeID == employeeId && s.TransDate == date && s.IsDeleted == false);

                if (isHoliday) continue;
                if (!employee.IsShift.Value)
                {
                    var dayOfWeek = date.DayOfWeek;

                    // Check working days (5 or 6 days a week)
                    if (employee.WorkingDays == 5 && (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday))
                        continue;

                    if (employee.WorkingDays == 6 && dayOfWeek == DayOfWeek.Sunday)
                        continue;
                }
                else
                {
                    // For shift workers, check their shift schedule
                    if (isNonWorkingShift) continue;
                }
                totalDays++;
            }

            return totalDays;
        }
        public async Task<Unattendances> CreateAsync(Unattendances data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var sequence = await _context.Unattendances.Where(x => x.StartDate.Month == data.StartDate.Month && x.StartDate.Year == data.StartDate.Year && x.IsDeleted == false).CountAsync();
                data.VoucherNo = string.Concat("SKT/", data.StartDate.ToString("yyMM"), (sequence + 1).ToString("D4"));
                data.Duration = await CalculateEffectiveDuration(data.StartDate, data.EndDate, data.EmployeeID);
                data.IsApproved1 = false;
                data.IsApproved2 = false;
                data.ApprovedDate1 = null;
                data.ApprovedDate2 = null;

                await _context.Unattendances.AddAsync(data);
                long UnattendanceID = await _context.SaveChangesAsync();

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
                var obj = await _context.Unattendances.FirstOrDefaultAsync(x => x.UnattendanceID == id && x.IsDeleted == false);
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

        public async Task<Unattendances> EditAsync(Unattendances data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Unattendances.FirstOrDefaultAsync(x => x.UnattendanceID == data.UnattendanceID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.StartDate = data.StartDate;
                obj.EndDate = data.EndDate;
                obj.UnattendanceTypeID = data.UnattendanceTypeID;
                obj.IsApproved1 = data.IsApproved1;
                obj.ApprovedDate1 = data.ApprovedDate1;
                obj.IsApproved2 = data.IsApproved2;
                obj.ApprovedDate2 = data.ApprovedDate2;
                obj.Description = data.Description;
                obj.Attachments = data.Attachments;
                obj.Duration = await CalculateEffectiveDuration(data.StartDate, data.EndDate, data.EmployeeID);

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
        public async Task<bool> ApprovalAsync(List<ApprovalDTO> data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var userid = Convert.ToInt64(User.FindFirstValue("id"));
                var approveddate = DateTime.Now;
                bool retval = false;
                int i = 0;
                foreach (var approval in data)
                {
                    var obj = await _context.Unattendances
                        .FirstOrDefaultAsync(x => x.UnattendanceID == approval.ID && x.IsDeleted == false);

                    if (obj != null)
                    {
                        if (approval.IsApproved1 != null)
                        {
                            obj.IsApproved1 = approval.IsApproved1;
                            obj.ApprovedBy1 = userid;
                            obj.ApprovedDate1 = approveddate;
                        }
                        if (approval.IsApproved2 != null)
                        {
                            obj.IsApproved2 = approval.IsApproved2;
                            obj.ApprovedBy2 = userid;
                            obj.ApprovedDate2 = approveddate;
                        }

                        obj.UserUp = userid;
                        obj.DateUp = approveddate;

                        i += await _context.SaveChangesAsync();
                    }
                }
                if (i > 0)
                    retval = true;

                await dbTrans.CommitAsync();

                return retval;
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
        public async Task<ListResponse<Unattendances>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from u in _context.Unattendances
                            join e in _context.Employees on u.EmployeeID equals e.EmployeeID
                            join ut in _context.UnattendanceTypes on u.UnattendanceTypeID equals ut.UnattendanceTypeID
                            join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                            from d in deptGroup.DefaultIfEmpty()
                            join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                            from di in divGroup.DefaultIfEmpty()
                            join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                            from g in groupGroup.DefaultIfEmpty()
                            where u.IsDeleted == false && (!u.IsApproved1.HasValue || !u.IsApproved2.HasValue)
                            select new Unattendances
                            {
                                UnattendanceID = u.UnattendanceID,
                                EmployeeID = u.EmployeeID,
                                StartDate = u.StartDate,
                                EndDate = u.EndDate,
                                UnattendanceTypeID = u.UnattendanceTypeID,
                                IsApproved1 = u.IsApproved1,
                                ApprovedBy1 = u.ApprovedBy1,
                                ApprovedDate1 = u.ApprovedDate1,
                                IsApproved2 = u.IsApproved2,
                                ApprovedBy2 = u.ApprovedBy2,
                                ApprovedDate2 = u.ApprovedDate2,
                                Description = u.Description,
                                NIK = e.Nik,
                                EmployeeName = e.EmployeeName,
                                DepartmentName = d.Name,
                                DepartmentID = e.DepartmentID,
                                DivisionID = e.DivisionID,
                                DivisionName = di != null ? di.Name : null,
                                GroupID = e.GroupID,
                                GroupName = g.Name,
                                GroupType = g.Type,
                                UnattendanceTypeCode = ut.Code,
                                UnattendanceTypeName = ut.Name,
                                Duration = u.Duration,
                                VoucherNo = u.VoucherNo
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Description.Contains(search) || x.UnattendanceTypeName.Contains(search) || x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.GroupName.Contains(search)
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
                            if (fieldName == "group" || fieldName == "department" || fieldName == "division" || fieldName == "unattendancetype")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "group")
                                    query = query.Where(x => Ids.Contains(x.GroupID ?? 0));
                                else if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                                else if (fieldName == "unattendancetype")
                                    query = query.Where(x => Ids.Contains(x.UnattendanceTypeID));
                            }
                            else
                                query = fieldName switch
                                {
                                    "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                    "voucher" => query.Where(x => x.VoucherNo.Contains(value)),
                                    "employeeid" => query.Where(x => x.EmployeeID.Equals(value)),
                                    _ => query
                                };
                        }
                    }
                }

                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                {
                    var dateRange = date.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    if (dateRange.Length == 2 && DateTime.TryParse(dateRange[0], out var startDate) && DateTime.TryParse(dateRange[1], out var endDate))
                        query = query.Where(x => (x.StartDate >= startDate && x.StartDate <= endDate || x.EndDate >= startDate && x.EndDate <= endDate));
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
                            "voucher" => query.OrderByDescending(x => x.VoucherNo),
                            "department" => query.OrderByDescending(x => x.DepartmentName),
                            "division" => query.OrderByDescending(x => x.DivisionName),
                            "group" => query.OrderByDescending(x => x.GroupType),
                            "unattendancetype" => query.OrderByDescending(x => x.UnattendanceTypeName),
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            "startdate" => query.OrderByDescending(x => x.StartDate),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "voucher" => query.OrderBy(x => x.VoucherNo),
                            "department" => query.OrderBy(x => x.DepartmentName),
                            "division" => query.OrderBy(x => x.DivisionName),
                            "group" => query.OrderBy(x => x.GroupType),
                            "unattendancetype" => query.OrderBy(x => x.UnattendanceTypeName),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            "startdate" => query.OrderBy(x => x.StartDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.UnattendanceID);
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

                return new ListResponse<Unattendances>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponse<Unattendances>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var employeeid = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var roleid = Convert.ToInt64(User.FindFirstValue("roleid"));
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from u in _context.Unattendances
                            join e in _context.Employees on u.EmployeeID equals e.EmployeeID
                            join ut in _context.UnattendanceTypes on u.UnattendanceTypeID equals ut.UnattendanceTypeID
                            join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                            from d in deptGroup.DefaultIfEmpty()
                            join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                            from di in divGroup.DefaultIfEmpty()
                            join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                            from g in groupGroup.DefaultIfEmpty()
                            where u.IsDeleted == false && ((u.EmployeeID == employeeid && roleid == 2) || (roleid != 2))
                            select new Unattendances
                            {
                                UnattendanceID = u.UnattendanceID,
                                EmployeeID = u.EmployeeID,
                                StartDate = u.StartDate,
                                EndDate = u.EndDate,
                                UnattendanceTypeID = u.UnattendanceTypeID,
                                IsApproved1 = u.IsApproved1,
                                ApprovedBy1 = u.ApprovedBy1,
                                ApprovedDate1 = u.ApprovedDate1,
                                IsApproved2 = u.IsApproved2,
                                ApprovedBy2 = u.ApprovedBy2,
                                ApprovedDate2 = u.ApprovedDate2,
                                Description = u.Description,
                                NIK = e.Nik,
                                EmployeeName = e.EmployeeName,
                                DepartmentName = d.Name,
                                DepartmentID = e.DepartmentID,
                                DivisionID = e.DivisionID,
                                DivisionName = di != null ? di.Name : null,
                                GroupID = e.GroupID,
                                GroupName = g.Name,
                                GroupType = g.Type,
                                UnattendanceTypeCode = ut.Code,
                                UnattendanceTypeName = ut.Name,
                                Duration = u.Duration,
                                VoucherNo = u.VoucherNo
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Description.Contains(search) || x.UnattendanceTypeName.Contains(search) || x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.GroupName.Contains(search)
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
                            if (fieldName == "group" || fieldName == "department" || fieldName == "division" || fieldName == "unattendancetype")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "group")
                                    query = query.Where(x => Ids.Contains(x.GroupID ?? 0));
                                else if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                                else if (fieldName == "unattendancetype")
                                    query = query.Where(x => Ids.Contains(x.UnattendanceTypeID));
                            }
                            else
                                query = fieldName switch
                                {
                                    "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                    "voucher" => query.Where(x => x.VoucherNo.Contains(value)),
                                    "employeeid" => query.Where(x => x.EmployeeID.Equals(value)),
                                    _ => query
                                };
                        }
                    }
                }

                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                {
                    var dateRange = date.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    if (dateRange.Length == 2 && DateTime.TryParse(dateRange[0], out var startDate) && DateTime.TryParse(dateRange[1], out var endDate))
                        query = query.Where(x => (x.StartDate >= startDate && x.StartDate <= endDate || x.EndDate >= startDate && x.EndDate <= endDate));
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
                            "voucher" => query.OrderByDescending(x => x.VoucherNo),
                            "department" => query.OrderByDescending(x => x.DepartmentName),
                            "division" => query.OrderByDescending(x => x.DivisionName),
                            "group" => query.OrderByDescending(x => x.GroupType),
                            "unattendancetype" => query.OrderByDescending(x => x.UnattendanceTypeName),
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            "startdate" => query.OrderByDescending(x => x.StartDate),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "voucher" => query.OrderBy(x => x.VoucherNo),
                            "department" => query.OrderBy(x => x.DepartmentName),
                            "division" => query.OrderBy(x => x.DivisionName),
                            "group" => query.OrderBy(x => x.GroupType),
                            "unattendancetype" => query.OrderBy(x => x.UnattendanceTypeName),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            "startdate" => query.OrderBy(x => x.StartDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.UnattendanceID);
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

                return new ListResponse<Unattendances>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Unattendances> GetByIdAsync(long id)
        {
            try
            {
                return await (from u in _context.Unattendances.AsNoTracking()
                              join e in _context.Employees on u.EmployeeID equals e.EmployeeID
                              join ut in _context.UnattendanceTypes on u.UnattendanceTypeID equals ut.UnattendanceTypeID
                              join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                              from d in deptGroup.DefaultIfEmpty()
                              join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                              from g in groupGroup.DefaultIfEmpty()
                              where u.UnattendanceID == id && u.IsDeleted == false
                              select new Unattendances
                              {
                                  UnattendanceID = u.UnattendanceID,
                                  EmployeeID = u.EmployeeID,
                                  StartDate = u.StartDate,
                                  EndDate = u.EndDate,
                                  UnattendanceTypeID = u.UnattendanceTypeID,
                                  IsApproved1 = u.IsApproved1,
                                  IsApproved2 = u.IsApproved2,
                                  Description = u.Description,
                                  NIK = e.Nik,
                                  EmployeeName = e.EmployeeName,
                                  DepartmentName = d.Name,
                                  DepartmentID = e.DepartmentID,
                                  GroupID = e.GroupID,
                                  GroupName = g.Name,
                                  GroupType = g.Type,
                                  UnattendanceTypeCode = ut.Code,
                                  UnattendanceTypeName = ut.Name,
                                  Duration = u.Duration,
                                  VoucherNo = u.VoucherNo
                              }).AsNoTracking().FirstOrDefaultAsync();
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
