using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using System.Security.Claims;
using sopra_hris_api.src.Entities;

namespace sopra_hris_api.src.Services.API
{
    public class OvertimeService : IServiceUnattendanceOVTAsync<Overtimes>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OvertimeService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<Overtimes> CreateAsync(Overtimes data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var sequence = await _context.Overtimes.Where(x => x.TransDate.Month == data.TransDate.Month && x.TransDate.Year == data.TransDate.Year && x.IsDeleted == false).CountAsync();
                data.VoucherNo = string.Concat("SPL/", data.TransDate.ToString("yyMM"), (sequence + 1).ToString("D4"));
                double roundedDownOvertime = Math.Floor(((data.EndDate - data.StartDate).TotalHours) * 2) / 2;
                data.OVTHours = (float?)roundedDownOvertime;
                data.IsApproved1 = false;
                data.IsApproved2 = false;
                data.ApprovedDate1 = null;
                data.ApprovedDate2 = null;

                await _context.Overtimes.AddAsync(data);
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
                var obj = await _context.Overtimes.FirstOrDefaultAsync(x => x.OvertimeID == id && x.IsDeleted == false);
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

        public async Task<Overtimes> EditAsync(Overtimes data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Overtimes.FirstOrDefaultAsync(x => x.OvertimeID == data.OvertimeID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.TransDate = data.TransDate;
                obj.StartDate = data.StartDate;
                obj.EndDate = data.EndDate;
                obj.ReasonID = data.ReasonID;
                obj.Description = data.Description;
                obj.IsApproved1 = data.IsApproved1;
                obj.ApprovedDate1 = data.ApprovedDate1;
                obj.IsApproved2 = data.IsApproved2;
                obj.ApprovedDate2 = data.ApprovedDate2;

                double roundedDownOvertime = Math.Floor(((data.EndDate - data.StartDate).TotalHours) * 2) / 2;
                data.OVTHours = (float?)roundedDownOvertime;
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
                    var obj = await _context.Overtimes
                        .FirstOrDefaultAsync(x => x.OvertimeID == approval.ID && x.IsDeleted == false);

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
        public async Task<ListResponse<Overtimes>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = (from o in _context.Overtimes.AsNoTracking()
                             join e in _context.Employees on o.EmployeeID equals e.EmployeeID
                             join r in _context.Reasons on o.ReasonID equals r.ReasonID into reasonGroup
                             from r in reasonGroup.DefaultIfEmpty()
                             join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                             from d in deptGroup.DefaultIfEmpty()
                             join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                             from di in divGroup.DefaultIfEmpty()
                             join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                             from g in groupGroup.DefaultIfEmpty()
                             where o.IsDeleted == false && (!o.IsApproved1.HasValue || !o.IsApproved2.HasValue)
                             select new Overtimes
                             {
                                 OvertimeID = o.OvertimeID,
                                 EmployeeID = o.EmployeeID,
                                 TransDate = o.TransDate,
                                 StartDate = o.StartDate,
                                 EndDate = o.EndDate,
                                 ReasonID = o.ReasonID,
                                 IsApproved1 = o.IsApproved1,
                                 ApprovedBy1 = o.ApprovedBy1,
                                 ApprovedDate1 = o.ApprovedDate1,
                                 IsApproved2 = o.IsApproved2,
                                 ApprovedBy2 = o.ApprovedBy2,
                                 ApprovedDate2 = o.ApprovedDate2,
                                 Description = o.Description,
                                 NIK = e.Nik,
                                 EmployeeName = e.EmployeeName,
                                 DepartmentID = e.DepartmentID,
                                 DepartmentName = d != null ? d.Name : null,
                                 DivisionID = e.DivisionID,
                                 DivisionName = di != null ? di.Name : null,
                                 GroupID = e.GroupID,
                                 GroupName = g != null ? g.Name : null,
                                 GroupType = g != null ? g.Type : null,
                                 ReasonCode = r != null ? r.Code : null,
                                 ReasonName = r != null ? r.Name : null,
                                 VoucherNo = o.VoucherNo
                             });

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Description.Contains(search) || x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.GroupName.Contains(search)
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
                            if (fieldName == "group" || fieldName == "department" || fieldName == "division" || fieldName == "reason")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "group")
                                    query = query.Where(x => Ids.Contains(x.GroupID ?? 0));
                                else if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                                else if (fieldName == "reason")
                                    query = query.Where(x => Ids.Contains(x.ReasonID ?? 0));
                            }
                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.Description.Contains(value)),
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
                        query = query.Where(x => (x.TransDate >= startDate && x.TransDate <= endDate ||
                         x.StartDate >= startDate && x.StartDate <= endDate || x.EndDate >= startDate && x.EndDate <= endDate));
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
                            "reason" => query.OrderByDescending(x => x.ReasonName),
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
                            "reason" => query.OrderBy(x => x.ReasonName),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            "startdate" => query.OrderBy(x => x.StartDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.OvertimeID);
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

                return new ListResponse<Overtimes>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponse<Overtimes>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var employeeid = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var roleid = Convert.ToInt64(User.FindFirstValue("roleid"));
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = (from o in _context.Overtimes.AsNoTracking()
                             join e in _context.Employees on o.EmployeeID equals e.EmployeeID
                             join r in _context.Reasons on o.ReasonID equals r.ReasonID into reasonGroup
                             from r in reasonGroup.DefaultIfEmpty()
                             join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                             from d in deptGroup.DefaultIfEmpty()
                             join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                             from di in divGroup.DefaultIfEmpty()
                             join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                             from g in groupGroup.DefaultIfEmpty()
                             where o.IsDeleted == false && ((o.EmployeeID == employeeid && roleid == 2) || (roleid != 2))
                             select new Overtimes
                             {
                                 OvertimeID = o.OvertimeID,
                                 EmployeeID = o.EmployeeID,
                                 TransDate = o.TransDate,
                                 StartDate = o.StartDate,
                                 EndDate = o.EndDate,
                                 ReasonID = o.ReasonID,
                                 IsApproved1 = o.IsApproved1,
                                 ApprovedBy1 = o.ApprovedBy1,
                                 ApprovedDate1 = o.ApprovedDate1,
                                 IsApproved2 = o.IsApproved2,
                                 ApprovedBy2 = o.ApprovedBy2,
                                 ApprovedDate2 = o.ApprovedDate2,
                                 Description = o.Description,
                                 NIK = e.Nik,
                                 EmployeeName = e.EmployeeName,
                                 DepartmentID = e.DepartmentID,
                                 DepartmentName = d != null ? d.Name : null,
                                 DivisionID = e.DivisionID,
                                 DivisionName = di != null ? di.Name : null,
                                 GroupID = e.GroupID,
                                 GroupName = g != null ? g.Name : null,
                                 GroupType = g != null ? g.Type : null,
                                 ReasonCode = r != null ? r.Code : null,
                                 ReasonName = r != null ? r.Name : null,
                                 VoucherNo = o.VoucherNo
                             });

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Description.Contains(search) || x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.GroupName.Contains(search)
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
                            if (fieldName == "group" || fieldName == "department" || fieldName == "division" || fieldName == "reason")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "group")
                                    query = query.Where(x => Ids.Contains(x.GroupID ?? 0));
                                else if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                                else if (fieldName == "reason")
                                    query = query.Where(x => Ids.Contains(x.ReasonID ?? 0));
                            }
                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.Description.Contains(value)),
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
                        query = query.Where(x => (x.TransDate >= startDate && x.TransDate <= endDate ||
                         x.StartDate >= startDate && x.StartDate <= endDate || x.EndDate >= startDate && x.EndDate <= endDate));
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
                            "reason" => query.OrderByDescending(x => x.ReasonName),
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
                            "reason" => query.OrderBy(x => x.ReasonName),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            "startdate" => query.OrderBy(x => x.StartDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.OvertimeID);
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

                return new ListResponse<Overtimes>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Overtimes> GetByIdAsync(long id)
        {
            try
            {
                return await (from o in _context.Overtimes.AsNoTracking()
                              join e in _context.Employees on o.EmployeeID equals e.EmployeeID
                              join r in _context.Reasons on o.ReasonID equals r.ReasonID into reasonGroup
                              from r in reasonGroup.DefaultIfEmpty()
                              join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                              from d in deptGroup.DefaultIfEmpty()
                              join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                              from g in groupGroup.DefaultIfEmpty()
                              where o.OvertimeID == id && o.IsDeleted == false
                              select new Overtimes
                              {
                                  OvertimeID = o.OvertimeID,
                                  EmployeeID = o.EmployeeID,
                                  TransDate = o.TransDate,
                                  StartDate = o.StartDate,
                                  EndDate = o.EndDate,
                                  ReasonID = o.ReasonID,
                                  IsApproved1 = o.IsApproved1,
                                  IsApproved2 = o.IsApproved2,
                                  Description = o.Description,
                                  NIK = e.Nik,
                                  EmployeeName = e.EmployeeName,
                                  DepartmentID = e.DepartmentID,
                                  DepartmentName = d != null ? d.Name : null,
                                  GroupID = e.GroupID,
                                  GroupName = g != null ? g.Name : null,
                                  GroupType = g != null ? g.Type : null,
                                  ReasonCode = r != null ? r.Code : null,
                                  ReasonName = r != null ? r.Name : null,
                                  VoucherNo = o.VoucherNo
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
