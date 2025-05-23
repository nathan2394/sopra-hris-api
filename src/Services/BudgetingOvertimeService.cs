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
    public class BudgetingOvertimeService : IServiceUnattendanceOVTAsync<BudgetingOvertimes>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BudgetingOvertimeService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

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
                    i += await _context.Database.ExecuteSqlRawAsync($@"UPDATE BudgetingOvertimes
                                        SET IsApproved1=CASE WHEN @IsApproved1 IS NOT NULL THEN @IsApproved1 ELSE IsApproved1 END,
                                            ApprovedBy1=CASE WHEN @IsApproved1 IS NOT NULL THEN @ApprovedBy1 ELSE ApprovedBy1 END,
                                            ApprovedDate1=CASE WHEN @IsApproved1 IS NOT NULL THEN @ApprovedDate1 ELSE ApprovedDate1 END,                                            
                                            ApprovalNotes=@ApprovalNotes,UserUp=@UserUp,DateUp=@DateUp
                                        WHERE VoucherNo=@VoucherNo AND IsDeleted=0",
                                        new SqlParameter("VoucherNo", approval.VoucherNo),
                                        new SqlParameter("IsApproved1", approval.IsApproved1 != null ? approval.IsApproved1 : (object)DBNull.Value),
                                        new SqlParameter("ApprovedBy1", approval.IsApproved1 != null ? userid : (object)DBNull.Value),
                                        new SqlParameter("ApprovedDate1", approval.IsApproved1 != null ? approveddate : (object)DBNull.Value),
                                        new SqlParameter("ApprovalNotes", approval.ApprovalNotes),
                                        new SqlParameter("UserUp", userid),
                                        new SqlParameter("DateUp", approveddate));

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

        public async Task<BudgetingOvertimes> CreateAsync(BudgetingOvertimes data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var budgeting_old = await _context.BudgetingOvertimes.FirstOrDefaultAsync(x => x.IsDeleted == false && x.BudgetYear == data.BudgetYear && x.BudgetMonth == data.BudgetMonth && x.DepartmentID == data.DepartmentID);
                if (budgeting_old == null)
                {
                    var sequence = await _context.BudgetingOvertimes.Where(x => x.BudgetMonth == data.BudgetMonth && x.BudgetYear == data.BudgetYear).CountAsync();
                    data.VoucherNo = string.Concat("BoT/", new DateTime(data.BudgetYear, 1, 1).ToString("yy"), new DateTime(DateTime.Now.Year, data.BudgetMonth, 1).ToString("MM"), (sequence + 1).ToString("D3"));
                    data.IsApproved1 = null;
                    data.ApprovalNotes = null;
                    data.ApprovedBy1 = null;
                    data.ApprovedDate1 = null;

                    await _context.BudgetingOvertimes.AddAsync(data);
                    long BudgetingOvertimesID = await _context.SaveChangesAsync();
                    var mailto = await _context.Set<EmailDTO>().FromSqlRaw(@"
        SELECT DISTINCT u.Email, u.Name
        FROM (
            SELECT DISTINCT o.DepartmentID, o.IsApproved1
            FROM BudgetingOvertimes o
            WHERE o.VoucherNo = @VoucherNo AND o.IsDeleted = 0
        ) AS t
        INNER JOIN MatrixApproval m ON t.DepartmentID = m.DepartmentID 
        INNER JOIN Users u ON u.EmployeeID = m.Releaser
                          AND u.IsDeleted = 0 
                          AND u.RoleID = 8
        WHERE m.IsDeleted = 0 
          AND t.IsApproved1 IS NULL", new SqlParameter("VoucherNo", data.VoucherNo)).FirstOrDefaultAsync();

                    if (mailto != null && !string.IsNullOrEmpty(mailto?.Email))
                    {
                        var departments = await _context.Departments.FirstOrDefaultAsync(x => x.DepartmentID == data.DepartmentID);
                        string subject = $"Pengajuan Budget Lembur {new DateTime(DateTime.Now.Year, data.BudgetMonth, 1):MMMM} {data.BudgetYear} - {data.VoucherNo}";
                        string body = $@"<!DOCTYPE html>
                                    <html>
                                      <body>
                                        <p>Dear <strong>{mailto.Name}</strong>,</p>

                                        <p>Mohon persetujuannya untuk pengajuan Budget Lembur {new DateTime(DateTime.Now.Year, data.BudgetMonth, 1):MMMM} {data.BudgetYear} berikut:</p>

                                        <p>
                                          <strong>Voucher No:</strong> {data.VoucherNo}<br>
                                          <strong>Department:</strong> {departments.Name}<br>
                                          <strong>Periode:</strong> {new DateTime(DateTime.Now.Year, data.BudgetMonth, 1):MMMM} {data.BudgetYear}<br>
                                        </p>

                                        <p>Terima kasih atas perhatian.</p>
                                      </body>
                                    </html>";
                        Utility.sendMail(String.Join(";", mailto.Email), "", subject, body);
                    }
                    await dbTrans.CommitAsync();

                    return data;
                }
                return budgeting_old;
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
                var obj = await _context.BudgetingOvertimes.FirstOrDefaultAsync(x => x.BudgetingOvertimesID == id && x.IsDeleted == false);
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

        public async Task<BudgetingOvertimes> EditAsync(BudgetingOvertimes data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.BudgetingOvertimes.FirstOrDefaultAsync(x => x.BudgetingOvertimesID == data.BudgetingOvertimesID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.BudgetMonth = data.BudgetMonth;
                obj.BudgetYear = data.BudgetYear;
                obj.TotalOvertimeHours= data.TotalOvertimeHours;
                obj.TotalOvertimeAmount = data.TotalOvertimeAmount;
                obj.RemainingHours = data.RemainingHours;
                obj.DepartmentID = data.DepartmentID;
                obj.IsApproved1 = data.IsApproved1;
                obj.ApprovedDate1 = data.ApprovedDate1;
                obj.ApprovedBy1 = data.ApprovedBy1;
                obj.ApprovalNotes = data.ApprovalNotes;
                obj.VoucherNo = data.VoucherNo;

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

        public async Task<ListResponse<BudgetingOvertimes>> GetAllApprovalAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                var EmployeeID = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var GroupID = Convert.ToInt64(User.FindFirstValue("groupid"));
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));

                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var matrixApproval = await _context.MatrixApproval.Where(x => x.IsDeleted == false).ToListAsync();
                var query = from u in _context.BudgetingOvertimes
                            join d in _context.Departments on u.DepartmentID equals d.DepartmentID into deptGroup
                            from d in deptGroup.DefaultIfEmpty()
                            where u.IsDeleted == false && (!u.IsApproved1.HasValue)
                            select new BudgetingOvertimes
                            {
                                BudgetingOvertimesID = u.BudgetingOvertimesID,
                                IsApproved1 = u.IsApproved1,
                                ApprovedBy1 = u.ApprovedBy1,
                                ApprovedDate1 = u.ApprovedDate1,
                                DepartmentName = d.Name,
                                DepartmentID = u.DepartmentID,
                                VoucherNo = u.VoucherNo,
                                BudgetMonth = u.BudgetMonth,
                                BudgetYear = u.BudgetYear,
                                TotalOvertimeAmount = u.TotalOvertimeAmount,
                                TotalOvertimeHours = u.TotalOvertimeHours,
                                RemainingHours = u.RemainingHours,
                                ApprovalNotes = u.ApprovalNotes
                            };
                if (RoleID == 8)//releaser
                {
                    matrixApproval = matrixApproval.Where(x => x.Releaser == EmployeeID).Distinct().ToList();

                    if (matrixApproval?.Count > 0)
                    {
                        var deptIds = matrixApproval.Select(x => x.DepartmentID).Distinct().ToList();
                        if (deptIds.Count > 0)
                            query = query.Where(x => deptIds.Contains(x.DepartmentID));

                        query = query.Where(x => !x.IsApproved1.HasValue);
                    }
                    else
                        query = query.Where(x => false);
                }
                else if (RoleID == 6 || RoleID == 7)
                    query = query.Where(x => false);
                // Searching
                //if (!string.IsNullOrEmpty(search))
                //    query = query.Where(x => x.Description.Contains(search) || x.UnattendanceTypeName.Contains(search) || x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.GroupName.Contains(search)
                //        );

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
                            if (fieldName == "department")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                 if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID));
                            }
                            if (fieldName == "month")
                            {
                                Int32.TryParse(value, out month);
                                query = query.Where(x => x.BudgetMonth == month);
                            }
                            else if (fieldName == "year")
                            {
                                Int32.TryParse(value, out year);
                                query = query.Where(x => x.BudgetYear == year);
                            }
                            else
                                query = fieldName switch
                                {
                                    "voucher" => query.Where(x => x.VoucherNo.Contains(value)),
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
                            "voucher" => query.OrderByDescending(x => x.VoucherNo),
                            "department" => query.OrderByDescending(x => x.DepartmentName),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "voucher" => query.OrderBy(x => x.VoucherNo),
                            "department" => query.OrderBy(x => x.DepartmentName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.BudgetingOvertimesID);
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

                return new ListResponse<BudgetingOvertimes>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponse<BudgetingOvertimes>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from b in _context.BudgetingOvertimes
                            join d in _context.Departments on b.DepartmentID equals d.DepartmentID
                            where b.IsDeleted == false
                            select new BudgetingOvertimes
                            {
                                BudgetingOvertimesID = b.BudgetingOvertimesID,
                                BudgetMonth = b.BudgetMonth,
                                BudgetYear = b.BudgetYear,
                                TotalOvertimeAmount = b.TotalOvertimeAmount,
                                TotalOvertimeHours = b.TotalOvertimeHours,
                                DepartmentID = b.DepartmentID,
                                DepartmentName = d.Name,
                                ApprovalNotes = b.ApprovalNotes,
                                ApprovedBy1 = b.ApprovedBy1,
                                ApprovedDate1 = b.ApprovedDate1,
                                IsApproved1 = b.IsApproved1,
                                VoucherNo = b.VoucherNo,
                                RemainingHours = b.RemainingHours
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.DepartmentName.Contains(search)
                        );

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

                            if (fieldName == "month")
                            {
                                Int32.TryParse(value, out month);
                                query = query.Where(x => x.BudgetMonth.Equals(month));
                            }
                            else if (fieldName == "year")
                            {
                                Int32.TryParse(value, out year);
                                query = query.Where(x => x.BudgetYear.Equals(year));
                            }

                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.DepartmentName.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.DepartmentName),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.DepartmentName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.BudgetingOvertimesID);
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

                return new ListResponse<BudgetingOvertimes>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<BudgetingOvertimes> GetByIdAsync(long id)
        {
            try
            {
                return await _context.BudgetingOvertimes.AsNoTracking().FirstOrDefaultAsync(x => x.BudgetingOvertimesID == id && x.IsDeleted == false);
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
