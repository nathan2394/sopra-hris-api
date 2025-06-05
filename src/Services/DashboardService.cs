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
    }
}
