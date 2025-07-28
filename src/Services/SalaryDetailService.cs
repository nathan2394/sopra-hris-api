using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace sopra_hris_api.src.Services.API
{
    public class SalaryDetailService : IServiceSalaryDetailsAsync<SalaryDetails>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SalaryDetailService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<ListResponse<SalaryDetails>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.SalaryDetails where a.IsDeleted == false select a;

                // Searching
                //if (!string.IsNullOrEmpty(search))
                    //query = query.Where(x => x.Name.Contains(search)
                        //);

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
                            query = fieldName switch
                            {
                                //"name" => query.Where(x => x.Name.Contains(value)),
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
                            //"name" => query.OrderByDescending(x => x.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            //"name" => query.OrderBy(x => x.Name),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.SalaryDetailID);
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

                return new ListResponse<SalaryDetails>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<SalaryDetails> GetByIdAsync(long id)
        {
            try
            {
                return await _context.SalaryDetails.AsNoTracking().FirstOrDefaultAsync(x => x.SalaryDetailID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponseTemplate<SalaryDetailReportsDTO>> GetSalaryDetailReports(string filter)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                // Prepare the SQL stored procedure call
                var parameters = new List<SqlParameter>();
                int month = DateTime.Now.Month;
                int year = DateTime.Now.Year;
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
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (fieldName == "month")
                                    Int32.TryParse(value, out month);
                                else if (fieldName == "year")
                                    Int32.TryParse(value, out year);
                            }
                        }
                    }
                }
                parameters.Add(new SqlParameter("@Month", SqlDbType.Int) { Value = month });
                parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = year });
                parameters.Add(new SqlParameter("@UserID", SqlDbType.Int) { Value = UserID });
                // Get Data
                var data = await _context.SalaryDetailReportsDTO.FromSqlRaw(
                  "EXEC usp_SalaryDetails @Month, @Year, @UserID", parameters.ToArray())
                  .ToListAsync();

                return new ListResponseTemplate<SalaryDetailReportsDTO>(data);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<SalaryDetailReportsDTO> GetSalaryDetails(long id)
        {
            try
            {
                //_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var parameters = new List<SqlParameter>();
                
                parameters.Add(new SqlParameter("@SalaryID", SqlDbType.BigInt) { Value = id });

                // Get Data
                var data = await _context.SalaryDetailReportsDTO.FromSqlRaw(
                  "EXEC usp_GetSalaryDetailsByID @SalaryID", parameters.ToArray())
                    .ToListAsync();

                if (data == null) return null;
                return data.FirstOrDefault();
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
