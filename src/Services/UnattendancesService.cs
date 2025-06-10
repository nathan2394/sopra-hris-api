using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;

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
                join s in _context.Shifts on e.ShiftID equals s.ShiftID into shiftGroup
                from s in shiftGroup.DefaultIfEmpty()
                                  where e.EmployeeID == employeeId && e.IsDeleted == false
                                  select new
                                  {
                                      e.IsShift,
                                      WorkingDays = s != null ? s.WorkingDays : null
                                  }).FirstOrDefaultAsync();
            if (employee == null)
                return 0;

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var isHoliday = await _context.Holidays.AnyAsync(h => h.TransDate == date && h.IsDeleted == false);
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
                totalDays++;
            }

            return totalDays;
        }
        public async Task<Unattendances> CreateAsync(Unattendances data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var ot_old = await _context.Unattendances.FirstOrDefaultAsync(x => x.IsDeleted == false && x.UnattendanceTypeID == data.UnattendanceTypeID
                && x.EmployeeID == data.EmployeeID && x.StartDate == data.StartDate && x.EndDate == data.EndDate);
                if (ot_old == null)
                {
                    int duration = await CalculateEffectiveDuration(data.StartDate, data.EndDate, data.EmployeeID);
                    var sequence = await _context.Unattendances.Where(x => x.StartDate.Month == data.StartDate.Month && x.StartDate.Year == data.StartDate.Year).CountAsync();
                    data.VoucherNo = string.Concat("SKT/", data.StartDate.ToString("yyMM"), (sequence + 1).ToString("D5"));
                    data.Duration = duration;
                    data.IsApproved1 = null;
                    data.IsApproved2 = null;
                    data.ApprovedBy1 = null;
                    data.ApprovedBy2 = null;
                    data.ApprovedDate1 = null;
                    data.ApprovedDate2 = null;
                    data.ApprovalNotes = null;

                    await _context.Unattendances.AddAsync(data);
                    long UnattendanceID = await _context.SaveChangesAsync();

                    await _context.Database.ExecuteSqlRawAsync(@"if exists (select 1 from EmployeeLeaveQuotas where EmployeeID=@EmployeeID and Year=@Year AND LeaveTypeID=2)
                                begin
                                update EmployeeLeaveQuotas
                                set UsedQuota=ISNULL(UsedQuota,0)+@Duration,DateUp=GETDATE(),UserUp=@UserID
                                where EmployeeID=@EmployeeID and Year=@Year AND LeaveTypeID=2
                                end", new SqlParameter("EmployeeID", data.EmployeeID), new SqlParameter("Year", data.StartDate.Year)
                                , new SqlParameter("Duration", duration), new SqlParameter("UserID", data.UserIn));

                    var mailto = await _context.Set<EmailDTO>().FromSqlRaw(@"
        SELECT DISTINCT u.Email, u.Name
        FROM (
            SELECT DISTINCT e.DepartmentID, e.DivisionID, o.IsApproved1, o.IsApproved2
            FROM Unattendances o
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
          AND t.IsApproved2 IS NULL", new SqlParameter("VoucherNo", data.VoucherNo)).FirstOrDefaultAsync();

                    if (mailto != null && !string.IsNullOrEmpty(mailto?.Email))
                    {
                        var types = await _context.UnattendanceTypes.FirstOrDefaultAsync(x => x.UnattendanceTypeID == data.UnattendanceTypeID);
                        string subject = $"Pengajuan Ketidakhadiran – {data.VoucherNo}";
                        string body = $@"<!DOCTYPE html>
                                    <html>
                                      <body>
                                        <p>Dear <strong>{mailto.Name}</strong>,</p>

                                        <p>Mohon persetujuannya untuk pengajuan ketidakhadiran berikut:</p>

                                        <p>
                                          <strong>Voucher No:</strong> {data.VoucherNo}<br>
                                          <strong>Tanggal:</strong> {data.StartDate.Date:dd MMM yyy} - {data.EndDate.Date:dd MMM yyy}<br>
                                          <strong>Jenis Ketidakhadiran:</strong> {types.Name}
                                        </p>

                                        <p>Terima kasih atas perhatian.</p>
                                      </body>
                                    </html>";
                        Utility.sendMail(String.Join(";", mailto.Email), "", subject, body);
                    }
                    await dbTrans.CommitAsync();

                    return data;
                }
                return ot_old;
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

                await _context.Database.ExecuteSqlRawAsync(@"update a
                                set UsedQuota=case when UsedQuota-b.Duration<0 then 0 else UsedQuota-b.Duration end,DateUp=GETDATE(),UserUp=@UserID
                                from EmployeeLeaveQuotas a
                                inner join Unattendances b on b.EmployeeID=a.EmployeeID AND a.Year=Year(b.StartDate)
                                where b.UnattendanceID=@UnattendanceID AND LeaveTypeID=2", new SqlParameter("UnattendanceID", id), new SqlParameter("UserID", UserID));

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
                obj.ApprovedBy1 = data.ApprovedBy1;
                obj.IsApproved2 = data.IsApproved2;
                obj.ApprovedDate2 = data.ApprovedDate2;
                obj.ApprovedBy2 = data.ApprovedBy2;
                obj.ApprovalNotes = data.ApprovalNotes;
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
                    i += await _context.Database.ExecuteSqlRawAsync($@"UPDATE Unattendances
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

                    if ((approval.IsApproved1.HasValue && approval.IsApproved1.Value == false) || (approval.IsApproved2.HasValue && approval.IsApproved2.Value == false))
                        await _context.Database.ExecuteSqlRawAsync(@"update a
                                set UsedQuota=case when UsedQuota-b.Duration<0 then 0 else UsedQuota-b.Duration end,DateUp=GETDATE(),UserUp=@UserID
                                from EmployeeLeaveQuotas a
                                inner join Unattendances b on b.EmployeeID=a.EmployeeID AND a.Year=Year(b.StartDate)
                                where b.VoucherNo=@VoucherNo AND LeaveTypeID=2", new SqlParameter("VoucherNo", approval.VoucherNo), new SqlParameter("UserID", userid));

                    var mailto = await _context.Set<EmailDTO>().FromSqlRaw(@"
SELECT DISTINCT u.Email, u.Name
        FROM (
            SELECT DISTINCT e.DepartmentID, e.DivisionID, o.IsApproved1, o.IsApproved2
            FROM Unattendances o
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
                        var ovt = await _context.Unattendances.FirstOrDefaultAsync(x => x.VoucherNo == approval.VoucherNo && x.IsDeleted == false);
                        var types = await _context.UnattendanceTypes.FirstOrDefaultAsync(x => x.UnattendanceTypeID == ovt.UnattendanceTypeID);
                        string subject = $"Pengajuan Ketidakhadiran – {ovt.VoucherNo}";
                        string body = $@"<!DOCTYPE html>
                                    <html>
                                      <body>
                                        <p>Dear <strong>{mailto.Name}</strong>,</p>

                                        <p>Mohon persetujuannya untuk pengajuan ketidakhadiran berikut:</p>

                                        <p>
                                          <strong>Voucher No:</strong> {ovt.VoucherNo}<br>
                                          <strong>Tanggal:</strong> {ovt.StartDate.Date:dd MMM yyy} - {ovt.EndDate.Date:dd MMM yyy}<br>
                                          <strong>Jenis Ketidakhadiran:</strong> {types.Name}
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
        public async Task<ListResponse<Unattendances>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                var EmployeeID = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var GroupID = Convert.ToInt64(User.FindFirstValue("groupid"));
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));

                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var matrixApproval = await _context.MatrixApproval.Where(x => x.IsDeleted == false).ToListAsync();
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
                                VoucherNo = u.VoucherNo,
                                Attachments = u.Attachments
                            };
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
                        query = query.Where(x => (x.StartDate.Date >= startDate.Date && x.StartDate.Date <= endDate.Date || x.EndDate.Date >= startDate.Date && x.EndDate.Date <= endDate.Date));
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
                    return await GetAllApprovalAsync(limit, page, total, search, sort, filter, date);
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
                            where u.IsDeleted == false && ((u.EmployeeID == employeeid && (roleid == 2 || roleid == 5)) || (roleid != 2 && roleid != 5))
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
                                VoucherNo = u.VoucherNo,
                                Attachments = u.Attachments
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
                        query = query.Where(x => (x.StartDate.Date >= startDate.Date && x.StartDate.Date <= endDate.Date || x.EndDate.Date >= startDate.Date && x.EndDate.Date <= endDate.Date));
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
