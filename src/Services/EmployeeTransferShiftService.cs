using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.src.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace sopra_hris_api.src.Services.API
{
    public class EmployeeTransferShiftService : IServiceEmployeeTransferShiftAsync<EmployeeTransferShifts>
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
                var sequence = await _context.EmployeeTransferShifts.Where(x => x.TransDate.Month == data.TransDate.Month && x.TransDate.Year == data.TransDate.Year).CountAsync();
                string voucherNo = $"TFS/{data.TransDate:yyMM}{(sequence + 1).ToString("D5")}";

                data.VoucherNo = voucherNo;
                data.IsApproved1 = null;
                data.IsApproved2 = null;
                data.ApprovedBy1 = null;
                data.ApprovedBy2 = null;
                data.ApprovedDate1 = null;
                data.ApprovedDate2 = null;
                data.ApprovalNotes = null;

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
        public async Task<string> BulkCreateAsync(BulkEmployeeTransferShifts data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var userid = Convert.ToInt64(User.FindFirstValue("id"));

                var sequence = await _context.EmployeeTransferShifts.Where(x => x.TransDate.Month == data.TransDate.Month && x.TransDate.Year == data.TransDate.Year).CountAsync();
                string voucherNo = !string.IsNullOrEmpty(data.VoucherNo) ? data.VoucherNo : $"TFS/{data.TransDate:yyMM}{(sequence + 1).ToString("D5")}";

                // Check if a record with the same voucher number already exists
                var existing = await _context.EmployeeTransferShifts.Where(x => x.VoucherNo == voucherNo && x.IsDeleted == false).CountAsync();
                if (existing > 0)
                {
                    // Mark the existing record as deleted
                    await _context.Database.ExecuteSqlRawAsync($@"UPDATE EmployeeTransferShifts SET IsDeleted=1,DateUp=GETDATE(),UserUp=@UserID WHERE VoucherNo=@VoucherNo AND IsDeleted=0",
                        new SqlParameter("VoucherNo", voucherNo),
                        new SqlParameter("userid", userid));
                }

                var employeeTransferShifts = new List<EmployeeTransferShifts>();
                foreach (var empID in data.EmployeeIDs)
                {
                    var transferShifts = new EmployeeTransferShifts()
                    {
                        VoucherNo = voucherNo,
                        EmployeeID = empID,
                        ShiftFromID = data.ShiftFromID,
                        ShiftToID = data.ShiftToID,
                        HourDiff = data.HourDiff,
                        TransDate = data.TransDate,
                        Remarks = data.Remarks,
                        IsApproved1 = null,
                        IsApproved2 = null,
                        ApprovedBy1 = null,
                        ApprovedBy2 = null,
                        ApprovedDate1 = null,
                        ApprovedDate2 = null,
                        UserIn = userid,
                        DateIn = DateTime.Now,
                        IsDeleted = false
                    };

                    employeeTransferShifts.Add(transferShifts);
                }

                await _context.EmployeeTransferShifts.AddRangeAsync(employeeTransferShifts);
                await _context.SaveChangesAsync();

                if (employeeTransferShifts.Count > 0) 
                {
                    var mailto = await _context.Set<EmailDTO>().FromSqlRaw(@"
        SELECT DISTINCT u.Email, u.Name
        FROM (
            SELECT DISTINCT e.DepartmentID, e.DivisionID, o.IsApproved1, o.IsApproved2
            FROM EmployeeTransferShifts o
            INNER JOIN Employees e ON e.EmployeeID = o.EmployeeID
            WHERE o.VoucherNo = @VoucherNo AND o.IsDeleted = 0
        ) AS t
        INNER JOIN MatrixApproval m ON t.DepartmentID = m.DepartmentID 
                                   AND (m.DivisionID = t.DivisionID OR m.DivisionID IS NULL)
        INNER JOIN Users u ON u.EmployeeID = 
                                (CASE 
                                    WHEN m.Checker IS NOT NULL THEN m.Checker 
                                    ELSE m.Releaser 
                                 END)
                          AND u.IsDeleted = 0 
                          AND u.RoleID = 
                                (CASE 
                                    WHEN m.Checker IS NOT NULL THEN 7 
                                    ELSE 8 
                                 END)
        WHERE m.IsDeleted = 0 
          AND t.IsApproved1 IS NULL 
          AND t.IsApproved2 IS NULL", new SqlParameter("VoucherNo", voucherNo)).FirstOrDefaultAsync();

                    if (mailto != null && !string.IsNullOrEmpty(mailto?.Email))
                    {
                        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.ShiftID == data.ShiftToID);
                        string shiftname = shift != null ? string.Concat(shift.Code, " - ", shift.Name) : "OFF";
                        string subject = $"Pengajuan Tukar Shift – {voucherNo}";
                        string body = $@"<!DOCTYPE html>
                                    <html>
                                      <body>
                                        <p>Dear <strong>{mailto.Name}</strong>,</p>

                                        <p>Mohon persetujuannya untuk pengajuan tukar shift berikut:</p>

                                        <p>
                                          <strong>Voucher No:</strong> {voucherNo}<br>
                                          <strong>Tanggal:</strong> {data.TransDate.Date:dd MMM yyy}<br>
                                          <strong>Shift:</strong> {shiftname}
                                        </p>

                                        <p>Terima kasih atas perhatian.</p>
                                      </body>
                                    </html>";
                        Utility.sendMail(String.Join(";", mailto.Email), "", subject, body);
                    }                
                }

                await dbTrans.CommitAsync();

                return voucherNo;
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

                obj.VoucherNo = data.VoucherNo;
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
                obj.ApprovalNotes = data.ApprovalNotes;

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
                    i += await _context.Database.ExecuteSqlRawAsync($@"UPDATE EmployeeTransferShifts
                                        SET IsApproved1=CASE WHEN @IsApproved1 IS NOT NULL THEN @IsApproved1 ELSE IsApproved1 END,
                                            ApprovedBy1=CASE WHEN @IsApproved1 IS NOT NULL THEN @ApprovedBy1 ELSE ApprovedBy1 END,
                                            ApprovedDate1=CASE WHEN @IsApproved1 IS NOT NULL THEN @ApprovedDate1 ELSE ApprovedDate1 END,
                                            IsApproved2=@IsApproved2,ApprovedBy2=@ApprovedBy2,ApprovedDate2=@ApprovedDate2,
                                            ApprovalNotes=@ApprovalNotes,UserUp=@UserUp,DateUp=@DateUp
                                        WHERE VoucherNo=@VoucherNo AND IsDeleted=0",
                                        new SqlParameter("VoucherNo", approval.VoucherNo),
                                        new SqlParameter("IsApproved1", approval.IsApproved1 != null ? approval.IsApproved1 : (object)DBNull.Value),
                                        new SqlParameter("ApprovedBy1", approval.IsApproved1 != null ? userid : (object)DBNull.Value),
                                        new SqlParameter("ApprovedDate1", approval.IsApproved1 != null ? approveddate : (object)DBNull.Value),
                                        new SqlParameter("IsApproved2", approval.IsApproved2 != null ? approval.IsApproved2 : (object)DBNull.Value),
                                        new SqlParameter("ApprovedBy2", approval.IsApproved2 != null ? userid : (object)DBNull.Value),
                                        new SqlParameter("ApprovedDate2", approval.IsApproved2 != null ? approveddate : (object)DBNull.Value),
                                        new SqlParameter("ApprovalNotes", approval.ApprovalNotes),
                                        new SqlParameter("UserUp", userid),
                                        new SqlParameter("DateUp", approveddate));

                    var mailto = await _context.Set<EmailDTO>().FromSqlRaw(@"
SELECT DISTINCT u.Email, u.Name
        FROM (
            SELECT DISTINCT e.DepartmentID, e.DivisionID, o.IsApproved1, o.IsApproved2
            FROM EmployeeTransferShifts o
            INNER JOIN Employees e ON e.EmployeeID = o.EmployeeID
            WHERE o.VoucherNo = @VoucherNo AND o.IsDeleted = 0
        ) AS t
        INNER JOIN MatrixApproval m ON t.DepartmentID = m.DepartmentID 
                                   AND (m.DivisionID = t.DivisionID OR m.DivisionID IS NULL)
        INNER JOIN Users u ON u.EmployeeID = m.Releaser 
                          AND u.IsDeleted = 0 
                          AND u.RoleID = 8 
        WHERE m.IsDeleted = 0 
          AND ISNULL(t.IsApproved1,-1)= case when m.Checker is null then -1 else 1 end
          AND t.IsApproved2 IS NULL
", new SqlParameter("VoucherNo", approval.VoucherNo)).FirstOrDefaultAsync();

                    if (mailto != null && !string.IsNullOrEmpty(mailto?.Email))
                    {
                        var ovt = await _context.EmployeeTransferShifts.FirstOrDefaultAsync(x => x.VoucherNo == approval.VoucherNo && x.IsDeleted == false);
                        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.ShiftID == ovt.ShiftToID);
                        string shiftname = shift != null ? string.Concat(shift.Code, " - ", shift.Name) : "OFF";
                        string subject = $"Pengajuan Tukar Shift – {ovt.VoucherNo}";
                        string body = $@"<!DOCTYPE html>
                                    <html>
                                      <body>
                                        <p>Dear <strong>{mailto.Name}</strong>,</p>

                                        <p>Mohon persetujuannya untuk pengajuan tukar shift berikut:</p>

                                        <p>
                                          <strong>Voucher No:</strong> {ovt.VoucherNo}<br>
                                          <strong>Tanggal:</strong> {ovt.TransDate.Date:dd MMM yyy}<br>
                                          <strong>Shift:</strong> {shiftname}
                                        </p>

                                        <p>Terima kasih atas perhatian.</p>
                                      </body>
                                    </html>";
                        Utility.sendMail(String.Join(";", mailto.Email), "", subject, body);
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
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                var EmployeeID = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var GroupID = Convert.ToInt64(User.FindFirstValue("groupid"));
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));

                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var matrixApproval = await _context.MatrixApproval.Where(x => x.IsDeleted == false).ToListAsync();
                var query = (from et in _context.EmployeeTransferShifts
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
                                 GroupType = g != null ? g.Type : null,
                                 VoucherNo = et.VoucherNo,
                                 Remarks = et.Remarks
                             });
                if (RoleID == 8)//releaser
                {
                    matrixApproval = matrixApproval.Where(x => x.Releaser == EmployeeID).Distinct().ToList();

                    if (matrixApproval?.Count > 0)
                    {
                        var deptIds = matrixApproval.Select(x => x.DepartmentID).Distinct().ToList();
                        if (deptIds.Count > 0)
                            query = query.Where(x => deptIds.Contains(x.DepartmentID ?? 0));
                        bool hasChecker = matrixApproval.Any(x => x.Checker != null);
                        if (hasChecker)
                            query = query.Where(x => x.IsApproved1.HasValue && x.IsApproved1.Value == true && !x.IsApproved2.HasValue);
                        else
                            query = query.Where(x => !x.IsApproved1.HasValue && !x.IsApproved2.HasValue);
                    }
                    else
                        query = query.Where(x => false);
                }
                else if (RoleID == 7)//checker
                {
                    matrixApproval = matrixApproval.Where(x => x.Checker == EmployeeID).Distinct().ToList();

                    if (matrixApproval?.Count > 0)
                    {
                        var deptIds = matrixApproval.Select(x => x.DepartmentID).Distinct().ToList();
                        if (deptIds.Count > 0)
                            query = query.Where(x => deptIds.Contains(x.DepartmentID ?? 0));

                        query = query.Where(x => !x.IsApproved1.HasValue);
                    }
                    else
                        query = query.Where(x => false);
                }
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
                                "name" => query.Where(x => x.EmployeeName.Contains(value)),
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
                        query = query.Where(x => (x.TransDate.Date >= startDate.Date && x.TransDate.Date <= endDate.Date));
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
                    return await GetAllApprovalAsync(limit, page, total, search, sort, filter, date);
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
                List<long> allowedEmployeeIds = new List<long>();
                if (roleid == 6)
                {
                    var currentEmployee = await (from e in _context.Employees
                                                 join g in _context.Groups on e.GroupID equals g.GroupID
                                                 where e.EmployeeID == employeeid
                                                 select new
                                                 {
                                                     Employee = e,
                                                     Group = g
                                                 }).FirstOrDefaultAsync();
                    if (currentEmployee != null)
                    {
                        var currentGroupLevel = currentEmployee.Group.Level;
                        var currentDeptId = currentEmployee.Employee.DepartmentID;

                        allowedEmployeeIds = await (from e in _context.Employees
                                                    join g in _context.Groups on e.GroupID equals g.GroupID
                                                    where g.Level < currentGroupLevel &&
                                                          e.DepartmentID == currentDeptId
                                                    select e.EmployeeID).ToListAsync();

                        allowedEmployeeIds.Add(currentEmployee.Employee.EmployeeID);
                    }
                }

                var query = (from et in _context.EmployeeTransferShifts
                             join e in _context.Employees on et.EmployeeID equals e.EmployeeID
                             join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                             from d in deptGroup.DefaultIfEmpty()
                             join di in _context.Divisions on e.DivisionID equals di.DivisionID into divGroup
                             from di in divGroup.DefaultIfEmpty()
                             join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                             from g in groupGroup.DefaultIfEmpty()
                             where et.IsDeleted == false && (allowedEmployeeIds.Count() > 0 ? (allowedEmployeeIds.Contains(et.EmployeeID)) : (et.EmployeeID == employeeid && (roleid == 2 || roleid == 5)) || (roleid != 2 && roleid != 5))
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
                                 GroupType = g != null ? g.Type : null,
                                 VoucherNo = et.VoucherNo,
                                 Remarks = et.Remarks
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
                                "name" => query.Where(x => x.EmployeeName.Contains(value)),
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
                        query = query.Where(x => (x.TransDate.Date >= startDate.Date && x.TransDate.Date <= endDate.Date));
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
