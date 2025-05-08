using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using System.Security.Claims;
using Azure.Core;
using Microsoft.Data.SqlClient;
using sopra_hris_api.src.Entities;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Text.RegularExpressions;

namespace sopra_hris_api.src.Services.API
{
    public class AttendanceService : IServiceAttendancesAsync<Attendances>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AttendanceService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<AttendanceDetails> SaveAttendancesAsync(AttendanceDTO attendance)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));                
                var employees = await _context.Employees.FirstOrDefaultAsync(x => x.EmployeeID == attendance.EmployeeID && x.IsDeleted == false);
                if (employees?.IsShift.Value == true)
                {
                    var obj = await _context.EmployeeShifts.FirstOrDefaultAsync(x => x.EmployeeID == attendance.EmployeeID && x.IsDeleted == false && x.TransDate.Value.Date == attendance.TransDate.Date);
                    if (obj != null)
                    {
                        obj.ShiftID = attendance.ShiftID;
                        obj.UserUp = UserID;
                        obj.DateUp = DateTime.Now;

                        await _context.SaveChangesAsync();
                    }
                }

                foreach (var item in attendance.Attendances)
                {
                    var existingAttendance = await _context.Attendances.FirstOrDefaultAsync(x => x.EmployeeID == attendance.EmployeeID && x.AttendanceID == item.AttendanceID);
                    if (existingAttendance == null)
                    {
                        var username = await _context.Users.Where(y => y.UserID == UserID).Select(x => x.Name).FirstOrDefaultAsync();
                        item.Description = "Created By: " + username;
                        item.UserIn = UserID;
                        item.DateIn = DateTime.Now;
                        item.IsDeleted = false;
                        await _context.Attendances.AddAsync(item);
                    }
                }

                var attendanceIds = attendance.Attendances.Select(a => a.AttendanceID).ToList();
                var recordsToDelete = await _context.Attendances
                    .Where(x => !attendanceIds.Contains(x.AttendanceID) && !string.IsNullOrEmpty(x.Description) && x.EmployeeID == attendance.EmployeeID && x.ClockIn.Date == attendance.TransDate.Date)
                    .ToListAsync();

                if (recordsToDelete.Any())
                    _context.Attendances.RemoveRange(recordsToDelete);
                
                await _context.SaveChangesAsync();

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Start", SqlDbType.DateTime) { Value = attendance.TransDate.Date },
                    new SqlParameter("@End", SqlDbType.DateTime) { Value = attendance.TransDate.Date },
                };
                var result = await _context.Database.ExecuteSqlRawAsync(
                  "EXEC usp_CalculateAttendance @Start, @End", parameters.ToArray());

                var data = await _context.AttendanceDetails.FirstOrDefaultAsync(x => x.EmployeeID == attendance.EmployeeID && x.TransDate.Date == attendance.TransDate.Date);
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
        public async Task<Attendances> CreateAsync(Attendances data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Attendances.AddAsync(data);
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
                var obj = await _context.Attendances.FirstOrDefaultAsync(x => x.AttendanceID == id && x.IsDeleted == false);
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
        public async Task<Attendances> EditAsync(Attendances data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Attendances.FirstOrDefaultAsync(x => x.AttendanceID == data.AttendanceID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.ClockIn = data.ClockIn;
                obj.Description = data.Description;
                obj.ProfilePhoto = data.ProfilePhoto;
                obj.Latitude = data.Latitude;
                obj.Longitude = data.Longitude;

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
        public async Task<ListResponse<Attendances>> GetAllAsync(int limit, int page, int total, long id, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                DateTime queryDate = DateTime.Now;

                if (!string.IsNullOrEmpty(date))
                    DateTime.TryParse(date, out queryDate);

                var query = _context.Attendances.FromSqlRaw(@"SELECT a.*
FROM Attendances a
LEFT JOIN EmployeeShifts b 
    ON b.EmployeeID = a.EmployeeID 
    AND b.IsDeleted = 0 
    AND CONVERT(DATE, b.TransDate) = @queryDate
WHERE a.EmployeeID = @id 
  AND (
    -- If there is a shift record
    (b.EmployeeShiftID IS NOT NULL AND (
        -- For night shift (ID = 3), handle overnight shift times
        (b.ShiftID = 3 AND (
            (CAST(a.ClockIn AS TIME) >= '18:00:00' AND CONVERT(DATE, a.ClockIn) = @queryDate)
            OR
            (CAST(a.ClockIn AS TIME) < '10:00:00' AND CONVERT(DATE, a.ClockIn) = DATEADD(DAY, 1, @queryDate))
        ))
        -- For other shifts, match normal day
        OR (b.ShiftID <> 3 AND CONVERT(DATE, a.ClockIn) = @queryDate)
    ))
    -- If there is no shift record, just use the date
    OR (b.EmployeeShiftID IS NULL AND CONVERT(DATE, a.ClockIn) = @queryDate)
  )", new SqlParameter("id", id), new SqlParameter("queryDate", queryDate));

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
                    return await GetAllAsync(limit, page, total, id, date);
                }

                return new ListResponse<Attendances>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponseTemplate<AttendanceSummary>> GetAllAsync(string filter, string date)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Database.SetCommandTimeout(300);
                var EmployeeID = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));

                DateTime dateNow = DateTime.Now;
                DateTime StartDate = new DateTime(dateNow.AddMonths(-1).Year, dateNow.AddMonths(-1).Month, 24);
                DateTime EndDate = new DateTime(dateNow.Year, dateNow.Month, 23);
                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                {
                    var dateRange = date.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    if (dateRange.Length == 2 && DateTime.TryParse(dateRange[0], out var startDate) && DateTime.TryParse(dateRange[1], out var endDate))
                    {
                        StartDate = startDate;
                        EndDate = endDate;
                    }
                }
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Start", SqlDbType.DateTime) { Value = StartDate },
                    new SqlParameter("@End", SqlDbType.DateTime) { Value = EndDate },
                };
                var data = await _context.Database.ExecuteSqlRawAsync(
                  "EXEC usp_CalculateAttendance @Start, @End", parameters.ToArray());

                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@EmployeeID", SqlDbType.BigInt) { Value = EmployeeID },
                    new SqlParameter("@RoleID", SqlDbType.BigInt) { Value = RoleID },
                    new SqlParameter("@Group", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("group", filter) },
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@Function", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("function", filter) },
                    new SqlParameter("@EmployeeType", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("employeetype", filter) },
                    new SqlParameter("@Division", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("division", filter) },
                    new SqlParameter("@Name", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("name", filter) },
                    new SqlParameter("@NIK", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("nik", filter) },
                    new SqlParameter("@KTP", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("ktp", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = _context.Set<AttendanceSummary>()
                    .FromSqlRaw("EXEC usp_GetEmployeeAttendanceSummary @StartDate, @EndDate, @EmployeeID, @RoleID, @Group, @Department, @Function, @EmployeeType, @Division, @Name, @NIK, @KTP", queryParameters.ToArray())
                    .ToList();

                await dbTrans.CommitAsync();

                return new ListResponseTemplate<AttendanceSummary>(result);
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
        public async Task<ListResponseTemplate<AttendanceCheck>> GetListCheckAsync(string filter, string date)
        {
            try
            {
                DateTime dateNow = DateTime.Now;
                DateTime StartDate = new DateTime(dateNow.AddMonths(-1).Year, dateNow.AddMonths(-1).Month, 24);
                DateTime EndDate = new DateTime(dateNow.Year, dateNow.Month, 23);
                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                {
                    var dateRange = date.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    if (dateRange.Length == 2 && DateTime.TryParse(dateRange[0], out var startDate) && DateTime.TryParse(dateRange[1], out var endDate))
                    {
                        StartDate = startDate;
                        EndDate = endDate;
                    }
                }
                var nameFilter = Utility.GetFilterValue("name", filter);
                var employeeIdFilter = Utility.GetFilterValue("employee", filter);
                var departmentIdFilter = Utility.GetFilterValue("department", filter);

                string formattedName = $"%{nameFilter}%";
                string formattedEmployeeId = string.Join(",", employeeIdFilter
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()));
                string formattedDepartmentId = string.Join(",", departmentIdFilter
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()));

                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var queryParameters = new List<SqlParameter> {                    
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate },
                    new SqlParameter("@Name", SqlDbType.NVarChar) { Value = formattedName },
                    new SqlParameter("@EmployeeID", SqlDbType.VarChar) { Value = formattedEmployeeId },
                    new SqlParameter("@Department", SqlDbType.VarChar) { Value = formattedDepartmentId }
                };
                var result = await _context.Set<AttendanceCheck>()
                    .FromSqlRaw($@"SELECT a.EmployeeID, a.NIK, a.EmployeeName, a.IsShift, TransDate, DayName, ShiftCode, ShiftName, Unattendance, ActualStartTime, ActualEndTime
FROM AttendanceDetails a
INNER JOIN Employees e on e.EmployeeID=a.EmployeeID
INNER JOIN Departments d on d.DepartmentID=e.DepartmentID
WHERE TransDate BETWEEN @StartDate AND @EndDate
	AND Unattendance IN ('H','A')
    AND a.EmployeeName LIKE @Name
    AND (@EmployeeID = '' OR a.EmployeeID IN (
        SELECT CAST(value AS BIGINT) 
        FROM STRING_SPLIT(@EmployeeID, ',') 
        WHERE RTRIM(LTRIM(value)) <> ''
      ))
    AND (@Department = '' OR e.DepartmentID IN (
        SELECT CAST(value AS BIGINT) 
        FROM STRING_SPLIT(@Department, ',') 
        WHERE RTRIM(LTRIM(value)) <> ''
      ))
ORDER BY a.EmployeeName, TransDate", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<AttendanceCheck>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponseTemplate<AttendanceDetails>> GetDetailAsync(long id, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                DateTime dateNow = DateTime.Now;
                DateTime StartDate = new DateTime(dateNow.AddMonths(-1).Year, dateNow.AddMonths(-1).Month, 24);
                DateTime EndDate = new DateTime(dateNow.Year, dateNow.Month, 23);
                if (!string.IsNullOrEmpty(date))
                {
                    var dateRange = date.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    if (dateRange.Length == 2 && DateTime.TryParse(dateRange[0], out var startDate) && DateTime.TryParse(dateRange[1], out var endDate))
                    {
                        StartDate = startDate;
                        EndDate = endDate;
                    }
                }

                var query = from ad in _context.AttendanceDetails
                            where ad.EmployeeID == id && (ad.TransDate >= StartDate && ad.TransDate <= EndDate)
                            select ad;
                var data = await query.ToListAsync();

                return new ListResponseTemplate<AttendanceDetails>(data);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<AttendanceShift>> GetDetailShiftsAsync(long id, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                DateTime queryDate = DateTime.Now;
                if (!string.IsNullOrEmpty(date))
                    DateTime.TryParse(date, out queryDate);

                var data = await _context.AttendanceShift.FromSqlRaw(@"SELECT ad.TransDate,e.IsShift, e.EmployeeID,
                        case when e.IsShift=1 then s.ShiftID else s2.ShiftID end ShiftID,
                        ad.ShiftCode,
						ad.ShiftName,                        
                        ad.StartTime AS StartTime,
                        ad.EndTime AS EndTime
                        FROM AttendanceDetails ad
                        INNER JOIN Employees e on e.EmployeeID=ad.EmployeeID AND e.IsDeleted=0
                        LEFT JOIN EmployeeShifts es on es.EmployeeID=ad.EmployeeID AND es.TransDate=ad.TransDate AND e.IsShift=1 AND es.IsDeleted=0
                        LEFT JOIN Shifts s on s.ShiftID=es.ShiftID AND s.IsDeleted=0
                        LEFT JOIN Shifts s2 on s2.ShiftID=e.ShiftID AND e.IsShift=0 AND s2.IsDeleted=0
                        WHERE ad.EmployeeID=@EmployeeID
	                        AND ad.TransDate=@TransDate", new SqlParameter("@EmployeeID", id), new SqlParameter("@TransDate", date)).ToListAsync();

                return new ListResponseTemplate<AttendanceShift>(data);
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
