using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class EmployeeIdeaService : IServiceAsync<EmployeeIdeas>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeIdeaService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<EmployeeIdeas> CreateAsync(EmployeeIdeas data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.SubmittedByUserID = data.UserIn;
                data.SubmittedDate = data.DateIn;
                await _context.EmployeeIdeas.AddAsync(data);
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
                var obj = await _context.EmployeeIdeas.FirstOrDefaultAsync(x => x.EmployeeIdeasID == id && x.IsDeleted == false);
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

        public async Task<EmployeeIdeas> EditAsync(EmployeeIdeas data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.EmployeeIdeas.FirstOrDefaultAsync(x => x.EmployeeIdeasID == data.EmployeeIdeasID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.Title = data.Title;
                obj.Description = data.Description;
                obj.Implementation = data.Implementation;
                obj.Impact = data.Impact;
                obj.SubmissionType = data.SubmissionType;
                obj.Status = data.Status;
                obj.EstimatedImplementationTime= data.EstimatedImplementationTime;
                obj.AttachmentLink = data.AttachmentLink;
                obj.ReviewDate = data.ReviewDate;
                obj.ReviewerComments = data.ReviewerComments;
                obj.TrialDate = data.TrialDate;
                obj.MonitoringEndDate = data.MonitoringEndDate;
                obj.ActualImplementationDetails = data.ActualImplementationDetails;
                obj.ImplementationDate = data.ImplementationDate;
                obj.ActualImpactDetails = data.ActualImpactDetails;

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


        public async Task<ListResponse<EmployeeIdeas>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));

                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.EmployeeIdeas
                            join user in _context.Users on a.SubmittedByUserID equals user.UserID into userJoin
                            from user in userJoin.DefaultIfEmpty()
                            join employee in _context.Employees on user.EmployeeID equals employee.EmployeeID into employeeJoin
                            from employee in employeeJoin.DefaultIfEmpty()
                            join department in _context.Departments on employee.DepartmentID equals department.DepartmentID into departmentJoin
                            from department in departmentJoin.DefaultIfEmpty()
                            where a.IsDeleted == false
                            select new EmployeeIdeas
                            {
                                EmployeeIdeasID = a.EmployeeIdeasID,
                                Title = a.Title,
                                Description = a.Description,
                                Implementation = a.Implementation,
                                Impact = a.Impact,
                                EstimatedImplementationTime = a.EstimatedImplementationTime,
                                AttachmentLink = a.AttachmentLink,
                                ReviewDate = a.ReviewDate,
                                ReviewerComments = a.ReviewerComments,
                                TrialDate = a.TrialDate,
                                MonitoringEndDate = a.MonitoringEndDate,
                                ActualImplementationDetails = a.ActualImplementationDetails,
                                ImplementationDate = a.ImplementationDate,
                                ActualImpactDetails = a.ActualImpactDetails,
                                SubmittedByUserID = a.SubmittedByUserID,
                                SubmittedDate = a.SubmittedDate,
                                SubmissionType = a.SubmissionType,
                                Status = a.Status,
                                EmployeeName = employee.EmployeeName,
                                DepartmentID = employee.DepartmentID,
                                DepartmentName = department.Name
                            };
                if (RoleID == 2 || RoleID == 5)
                    query = query.Where(x => x.SubmittedByUserID == UserID);
                
                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeeName.Contains(search) || x.Title.Contains(search)
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
                            if (fieldName == "department")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                            }
                            else
                                query = fieldName switch
                                {
                                    "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                    "title" => query.Where(x => x.Title.Contains(value)),
                                    "status" => query.Where(x => x.Status.Contains(value)),
                                    "submittedby" => query.Where(x => x.SubmittedByUserID.Equals(value)),
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
                        query = query.Where(x => (x.SubmittedDate.Value.Date >= startDate.Date && x.SubmittedDate.Value.Date <= endDate.Date));
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
                    query = query.OrderByDescending(x => x.EmployeeIdeasID);
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

                return new ListResponse<EmployeeIdeas>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<EmployeeIdeas> GetByIdAsync(long id)
        {
            try
            {
                return await _context.EmployeeIdeas.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeIdeasID == id && x.IsDeleted == false);
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
