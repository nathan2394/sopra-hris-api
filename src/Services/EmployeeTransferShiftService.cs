using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.src.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace sopra_hris_api.src.Services.API
{
    public class EmployeeTransferShiftService : IServiceUnattendanceOVTAsync<EmployeeTransferShifts>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeTransferShiftService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<EmployeeTransferShifts> CreateAsync(EmployeeTransferShifts data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.IsApproved1 = null;
                data.IsApproved2 = null;
                data.ApprovedBy1 = null;
                data.ApprovedBy2 = null;
                data.ApprovedDate1 = null;
                data.ApprovedDate2 = null;
                await _context.EmployeeTransferShifts.AddAsync(data);
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
                var obj = await _context.EmployeeTransferShifts.FirstOrDefaultAsync(x => x.EmployeeTransferShiftID == id && x.IsDeleted == false);
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

        public async Task<EmployeeTransferShifts> EditAsync(EmployeeTransferShifts data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.EmployeeTransferShifts.FirstOrDefaultAsync(x => x.EmployeeTransferShiftID == data.EmployeeTransferShiftID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.ShiftFromID = data.ShiftFromID;
                obj.ShiftToID = data.ShiftToID;
                obj.TransDate = data.TransDate;
                obj.HourDiff = data.HourDiff;
                obj.Remarks = data.Remarks;
                obj.IsApproved1 = data.IsApproved1;
                obj.IsApproved2 = data.IsApproved2;
                obj.ApprovedBy1 = data.ApprovedBy1;
                obj.ApprovedBy2 = data.ApprovedBy2;
                obj.ApprovedDate1 = data.ApprovedDate1;
                obj.ApprovedDate2 = data.ApprovedDate2;

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
                    var obj = await _context.EmployeeTransferShifts
                        .FirstOrDefaultAsync(x => x.EmployeeTransferShiftID == approval.ID && x.IsDeleted == false);

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
        public async Task<ListResponse<EmployeeTransferShifts>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = (from et in _context.EmployeeTransferShifts.AsNoTracking()
                             join e in _context.Employees on et.EmployeeID equals e.EmployeeID
                             join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                             from d in deptGroup.DefaultIfEmpty()
                             join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                             from di in divGroup.DefaultIfEmpty()
                             join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                             from g in groupGroup.DefaultIfEmpty()
                             where et.IsDeleted == false && ((et.IsApproved1 ?? false) == false || (et.IsApproved2 ?? false) == false)
                             select new EmployeeTransferShifts
                             {
                                 EmployeeTransferShiftID = et.EmployeeTransferShiftID,
                                 EmployeeID = et.EmployeeID,
                                 TransDate = et.TransDate,
                                 ShiftFromID = et.ShiftFromID,
                                 ShiftToID = et.ShiftToID,
                                 HourDiff = et.HourDiff,
                                 IsApproved1 = et.IsApproved1,
                                 ApprovedBy1 = et.ApprovedBy1,
                                 ApprovedDate1 = et.ApprovedDate1,
                                 IsApproved2 = et.IsApproved2,
                                 ApprovedBy2 = et.ApprovedBy2,
                                 ApprovedDate2 = et.ApprovedDate2,
                                 NIK = e.Nik,
                                 EmployeeName = e.EmployeeName,
                                 DepartmentID = e.DepartmentID,
                                 DepartmentName = d != null ? d.Name : null,
                                 DivisionID = e.DivisionID,
                                 DivisionName = di != null ? di.Name : null,
                                 GroupID = e.GroupID,
                                 GroupName = g != null ? g.Name : null,
                                 GroupType = g != null ? g.Type : null
                             });

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.DivisionName.Contains(search) || x.GroupName.Contains(search)
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
                            if (fieldName == "group" || fieldName == "department" || fieldName == "division")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "group")
                                    query = query.Where(x => Ids.Contains(x.GroupID ?? 0));
                                else if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                            }
                            query = fieldName switch
                            {
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
                        query = query.Where(x => (x.TransDate >= startDate && x.TransDate <= endDate));
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
                            "group" => query.OrderByDescending(x => x.GroupType),
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
                            "group" => query.OrderBy(x => x.GroupType),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.EmployeeTransferShiftID);
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

                return new ListResponse<EmployeeTransferShifts>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponse<EmployeeTransferShifts>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var employeeid = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var roleid = Convert.ToInt64(User.FindFirstValue("roleid"));
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = (from et in _context.EmployeeTransferShifts.AsNoTracking()
                             join e in _context.Employees on et.EmployeeID equals e.EmployeeID
                             join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                             from d in deptGroup.DefaultIfEmpty()
                             join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                             from di in divGroup.DefaultIfEmpty()
                             join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                             from g in groupGroup.DefaultIfEmpty()
                             where et.IsDeleted == false && ((et.EmployeeID == employeeid && roleid == 2) || (roleid != 2))
                             select new EmployeeTransferShifts
                             {
                                 EmployeeTransferShiftID = et.EmployeeTransferShiftID,
                                 EmployeeID = et.EmployeeID,
                                 TransDate = et.TransDate,
                                 ShiftFromID = et.ShiftFromID,
                                 ShiftToID = et.ShiftToID,
                                 HourDiff = et.HourDiff,
                                 IsApproved1 = et.IsApproved1,
                                 ApprovedBy1 = et.ApprovedBy1,
                                 ApprovedDate1 = et.ApprovedDate1,
                                 IsApproved2 = et.IsApproved2,
                                 ApprovedBy2 = et.ApprovedBy2,
                                 ApprovedDate2 = et.ApprovedDate2,
                                 NIK = e.Nik,
                                 EmployeeName = e.EmployeeName,
                                 DepartmentID = e.DepartmentID,
                                 DepartmentName = d != null ? d.Name : null,
                                 DivisionID = e.DivisionID,
                                 DivisionName = di != null ? di.Name : null,
                                 GroupID = e.GroupID,
                                 GroupName = g != null ? g.Name : null,
                                 GroupType = g != null ? g.Type : null
                             });
                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.DivisionName.Contains(search) || x.GroupName.Contains(search)
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
                            if (fieldName == "group" || fieldName == "department" || fieldName == "division")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "group")
                                    query = query.Where(x => Ids.Contains(x.GroupID ?? 0));
                                else if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                            }
                            query = fieldName switch
                            {
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
                            "group" => query.OrderByDescending(x => x.GroupType),
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
                            "group" => query.OrderBy(x => x.GroupType),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.EmployeeTransferShiftID);
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

                return new ListResponse<EmployeeTransferShifts>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<EmployeeTransferShifts> GetByIdAsync(long id)
        {
            try
            {
                return await _context.EmployeeTransferShifts.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeTransferShiftID == id && x.IsDeleted == false);
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
