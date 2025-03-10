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


        public async Task<ListResponseTemplate<AttendanceSummary>> GetAllAsync(string filter, string date)
        {
            try
            {
                var EmployeeID = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));

                DateTime StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-24);
                DateTime EndDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 23);
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

                var data = await _context.AttendanceSummary.FromSqlRaw(
                  "EXEC usp_CalculateAttendance @Start, @End", parameters.ToArray())
                  .ToListAsync();

                return new ListResponseTemplate<AttendanceSummary>(data);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Attendances> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Attendances.AsNoTracking().FirstOrDefaultAsync(x => x.AttendanceID == id && x.IsDeleted == false);
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
