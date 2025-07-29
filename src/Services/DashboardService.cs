using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;
using static sopra_hris_api.src.Entities.DashboardDTO;

namespace sopra_hris_api.src.Services.API
{
    public class DashboardService : IServiceDashboardAsync<DashboardDTO>
    {
        private readonly EFContext _context;

        public DashboardService(EFContext context)
        {
            _context = context;
        }

        public async Task<ListResponseTemplate<DashboardApproval>> GetApproval(string filter, string date)
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardApproval>()
                    .FromSqlRaw("EXEC usp_Dashboard_Approval @StartDate, @EndDate, @Department", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardApproval>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponseTemplate<DashboardAttendanceByShift>> GetAttendanceByShift(string filter, string date)
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardAttendanceByShift>()
                    .FromSqlRaw("EXEC usp_Dashboard_AttendanceByShift @StartDate, @EndDate, @Department", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardAttendanceByShift>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponseTemplate<DashboardAttendanceNormalAbnormal>> GetAttendanceNormalAbnormal(string filter, string date)
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardAttendanceNormalAbnormal>()
                    .FromSqlRaw("EXEC usp_Dashboard_AttendanceNormalAbnormal @StartDate, @EndDate, @Department", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardAttendanceNormalAbnormal>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponseTemplate<DashboardAttendanceSummary>> GetAttendanceSummary(string filter, string date)
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardAttendanceSummary>()
                    .FromSqlRaw("EXEC usp_Dashboard_AttendanceSummary @StartDate, @EndDate, @Department", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardAttendanceSummary>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponseTemplate<DashboardBudgetOvertimes>> GetBudgetOvertimes(string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardBudgetOvertimes>()
                    .FromSqlRaw("EXEC usp_Dashboard_BudgetOvertimes @StartDate, @EndDate, @Department", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardBudgetOvertimes>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<DashboardDetaillOVT>> GetDetailOVT(string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardDetaillOVT>()
                    .FromSqlRaw(@"SELECT e.EmployeeID,e.Nik,e.EmployeeName,e.StartWorkingDate,a.TransDate,d.DepartmentID,d.Name Department,CONVERT(real,ISNULL(Ovt,0))/60 OVT
FROM AttendanceDetails a
INNER JOIN Employees e on e.EmployeeID=a.EmployeeID AND e.IsDeleted=0 AND (endworkingdate is null OR EndWorkingDate>=@startDate)
INNER JOIN Departments d on d.DepartmentID=e.DepartmentID AND d.IsDeleted=0
WHERE CONVERT(date,TransDate) BETWEEN @startDate and @endDate
	AND (e.DepartmentID IN (SELECT value FROM STRING_SPLIT(@department, ',')) or @department='')
	AND OVT>0", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardDetaillOVT>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<DashboardDetaillMeals>> GetDetailMeals(string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardDetaillMeals>()
                    .FromSqlRaw(@"SELECT e.EmployeeID,e.Nik,e.EmployeeName,e.StartWorkingDate,a.TransDate,d.DepartmentID,d.Name Department
FROM AttendanceDetails a
INNER JOIN Employees e on e.EmployeeID=a.EmployeeID AND e.IsDeleted=0 AND (endworkingdate is null OR EndWorkingDate>=@startDate)
INNER JOIN Departments d on d.DepartmentID=e.DepartmentID AND d.IsDeleted=0
WHERE CONVERT(date,TransDate) BETWEEN @startDate and @endDate
	AND (e.DepartmentID IN (SELECT value FROM STRING_SPLIT(@department, ',')) or @department='')
	AND Meals>0", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardDetaillMeals>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<DashboardDetaillAbsent>> GetDetaillAbsent(string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardDetaillAbsent>()
                    .FromSqlRaw(@"SELECT e.EmployeeID,e.Nik,e.EmployeeName,e.StartWorkingDate,a.TransDate,d.DepartmentID,d.Name Department,Unattendance
FROM AttendanceDetails a
INNER JOIN Employees e on e.EmployeeID=a.EmployeeID AND e.IsDeleted=0 AND (endworkingdate is null OR EndWorkingDate>=@startDate)
INNER JOIN Departments d on d.DepartmentID=e.DepartmentID AND d.IsDeleted=0
WHERE CONVERT(date,TransDate) BETWEEN @startDate and @endDate
	AND (e.DepartmentID IN (SELECT value FROM STRING_SPLIT(@department, ',')) or @department='')
	AND a.ShiftCode is not null 
	AND a.Unattendance in ('A','H','CT1','CU','CL','CI','CM','CG','CK','CKM','I','SKD','DL')", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardDetaillAbsent>(result);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<DashboardDetaillLate>> GetDetailLate(string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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

                var queryParameters = new List<SqlParameter> {
                    new SqlParameter("@Department", SqlDbType.NVarChar) { Value = Utility.GetFilterValue("department", filter) },
                    new SqlParameter("@StartDate", SqlDbType.Date) { Value = StartDate },
                    new SqlParameter("@EndDate", SqlDbType.Date) { Value = EndDate }
                };
                var result = await _context.Set<DashboardDetaillLate>()
                    .FromSqlRaw(@"SELECT e.EmployeeID,e.Nik,e.EmployeeName,e.StartWorkingDate,a.TransDate,d.DepartmentID,d.Name Department,a.StartTime,a.EndTime,a.ActualStartTime,a.ActualEndTime
FROM AttendanceDetails a
INNER JOIN Employees e on e.EmployeeID=a.EmployeeID AND e.IsDeleted=0 AND (endworkingdate is null OR EndWorkingDate>=@startDate)
INNER JOIN Departments d on d.DepartmentID=e.DepartmentID AND d.IsDeleted=0
WHERE CONVERT(date,TransDate) BETWEEN @startDate and @endDate
	AND (e.DepartmentID IN (SELECT value FROM STRING_SPLIT(@department, ',')) or @department='')
	AND Late>0", queryParameters.ToArray())
                    .ToListAsync();

                return new ListResponseTemplate<DashboardDetaillLate>(result);
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
