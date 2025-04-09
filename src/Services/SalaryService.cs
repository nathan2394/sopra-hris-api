using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.src.Entities;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace sopra_hris_api.src.Services.API
{
    public class SalaryService : IServiceSalaryAsync<Salary>
    {
        private readonly EFContext _context;

        public SalaryService(EFContext context)
        {
            _context = context;
        }
        public async Task<ListResponse<Salary>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = (from salary in _context.Salary
                             join employee in _context.Employees on salary.EmployeeID equals employee.EmployeeID
                             join employeeType in _context.EmployeeTypes on employee.EmployeeTypeID equals employeeType.EmployeeTypeID
                             join groups in _context.Groups on employee.GroupID equals groups.GroupID
                             join function in _context.Functions on employee.FunctionID equals function.FunctionID
                             join department in _context.Departments on employee.DepartmentID equals department.DepartmentID
                             join division in _context.Divisions on employee.DivisionID equals division.DivisionID
                             where salary.IsDeleted == false
                             select new Salary
                             {
                                 SalaryID = salary.SalaryID,
                                 EmployeeID = salary.EmployeeID,
                                 Nik = employee.Nik,
                                 Name = employee.EmployeeName,
                                 EmployeeTypeID = employee.EmployeeTypeID,
                                 EmployeeTypeName = employeeType.Name,
                                 GroupID = groups.GroupID,
                                 GroupName = groups.Name,
                                 FunctionID = function.FunctionID,
                                 FunctionName = function.Name,
                                 DivisionID = division.DivisionID,
                                 DivisionName = division.Name,
                                 DepartmentID = department.DepartmentID,
                                 DepartmentName = department.Name,
                                 Month = salary.Month,
                                 Year = salary.Year,
                                 HKS = salary.HKS,
                                 HKA = salary.HKA,
                                 ATT = salary.ATT,
                                 OVT = salary.OVT,
                                 Late = salary.Late,
                                 MEAL = salary.MEAL,
                                 ABSENT = salary.ABSENT,
                                 AllowanceTotal = salary.AllowanceTotal,
                                 DeductionTotal = salary.DeductionTotal,
                                 Netto = salary.Netto,
                                 PayrollType = salary.PayrollType
                             });
                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Contains(search)
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
                            "name" => query.OrderByDescending(x => x.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.Name),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.SalaryID);
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

                return new ListResponse<Salary>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<SalaryCalculatorModel>> SetCalculator(SalaryCalculatorTemplate request)
        {
            try
            {
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@HKS", SqlDbType.BigInt) { Value = request.HKS ?? 0L },
                    new SqlParameter("@HKA", SqlDbType.BigInt) { Value = request.HKA ?? 0L },
                    new SqlParameter("@ATT", SqlDbType.BigInt) { Value = request.ATT ?? 0L },
                    new SqlParameter("@OVT", SqlDbType.Decimal) { Value = request.OVT ?? 0 },
                    new SqlParameter("@MEAL", SqlDbType.BigInt) { Value = request.MEAL ?? 0L },
                    new SqlParameter("@StartJointDate", SqlDbType.Date) { Value = request.StartJointDate ?? (object) DBNull.Value },
                    new SqlParameter("@BasicSalary", SqlDbType.Decimal) { Value = request.BasicSalary ?? 0 },
                    new SqlParameter("@GroupID", SqlDbType.BigInt) { Value = request.GroupID ?? 0L },
                    new SqlParameter("@PayrollType", SqlDbType.VarChar) { Value = request.PayrollType ?? "" },
                    new SqlParameter("@BPJS", SqlDbType.Decimal) { Value = request.BPJS ?? 0 },
                    new SqlParameter("@Operational", SqlDbType.Decimal) { Value = request.Operational ?? 0 },
                    new SqlParameter("@Khusus", SqlDbType.Decimal) { Value = request.Khusus ?? 0 },
                    new SqlParameter("@Functional", SqlDbType.Decimal) { Value = request.Functional ?? 0 },
                };

                var data = await _context.SalaryCalculatorModel.FromSqlRaw(
                  "EXEC usp_Calculator @HKS, @HKA, @ATT, @OVT, @MEAL, @StartJointDate, @BasicSalary, @GroupID, @PayrollType, @BPJS, @Operational, @Khusus, @Functional", parameters.ToArray())
                  .ToListAsync();
                return new ListResponseTemplate<SalaryCalculatorModel>(data);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        private DataTable ToSalaryTemplateTypeDataTable(List<SalaryTemplateDTO> template)
        {
            var table = new DataTable();
            table.Columns.Add("EmployeeID", typeof(int));
            table.Columns.Add("NIK", typeof(string));
            table.Columns.Add("EMPLOYEENAME", typeof(string));
            table.Columns.Add("Month", typeof(int));
            table.Columns.Add("Year", typeof(int));
            table.Columns.Add("HKS", typeof(int));
            table.Columns.Add("HKA", typeof(int));
            table.Columns.Add("ATT", typeof(int));
            table.Columns.Add("MEAL", typeof(int));
            table.Columns.Add("ABSENT", typeof(int));
            table.Columns.Add("Late", typeof(int));
            table.Columns.Add("OVT", typeof(decimal));
            table.Columns.Add("Rapel", typeof(decimal));
            table.Columns.Add("OtherAllowances", typeof(decimal));
            table.Columns.Add("OtherDeductions", typeof(decimal));

            foreach (var item in template)
            {
                table.Rows.Add(
                    item.EmployeeID,
                    item.Nik,
                    item.Name,
                    item.Month,
                    item.Year,
                    item.HKS,
                    item.HKA,
                    item.ATT,
                    item.MEAL,
                    item.ABSENT,
                    item.Late,
                    item.OVT,
                    item.Rapel,
                    item.OtherAllowances,
                    item.OtherDeductions
                );
            }

            return table;
        }
        public async Task<ListResponseUploadTemplate<SalaryDetailReportsDTO>> GetSalaryResultPayrollAsync(List<SalaryTemplateDTO> template, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var templateTable = ToSalaryTemplateTypeDataTable(template);
                var templateParameter = new SqlParameter("@Template", SqlDbType.Structured)
                {
                    TypeName = "dbo.SalaryTemplateType",
                    Value = templateTable
                };
                await _context.Database.ExecuteSqlRawAsync(
                    $"EXEC usp_GetSalaryResultPayroll @Month = {template.FirstOrDefault()?.Month}, @Year = {template.FirstOrDefault()?.Year}, @Template = @Template, @UserID = {UserID}",
                    templateParameter
                );

                await dbTrans.CommitAsync();

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Month", SqlDbType.Int) { Value = template.FirstOrDefault()?.Month },
                    new SqlParameter("@Year", SqlDbType.Int) { Value = template.FirstOrDefault()?.Year },
                    new SqlParameter("@IsFlag", SqlDbType.Int) { Value = 1 },
                    new SqlParameter("@UserID", SqlDbType.BigInt) { Value = UserID }
                };
                var salaryDataList = await _context.SalaryDetailReportsDTO.FromSqlRaw(
                  "EXEC usp_SalaryDetails @Month, @Year, @IsFlag, @UserID", parameters.ToArray())
                  .ToListAsync();

                var salaryPayrollSummary = await _context.SalaryPayrollSummaryDTO.FromSqlRaw($@"select d.Name [DepartmentName]
	                    , SUM(s.Netto) [AmountTransfer]
	                    , COUNT(e.EmployeeID) [CountEmployee]
	                    , CONVERT(decimal,SUM(s.Netto))/COUNT(e.EmployeeID) [AVGAmountEmployee]
                    from Salary s
                    inner join Employees e on e.EmployeeID=s.EmployeeID
                    left join Departments d on d.DepartmentID=e.DepartmentID
                    where s.IsDeleted=0
	                    and s.Month={template.FirstOrDefault()?.Month}
	                    and s.Year={template.FirstOrDefault()?.Year}
                    group by d.Name").ToListAsync();

                var salaryPayrollSummaryTotal = await _context.SalaryPayrollSummaryTotalDTO.FromSqlRaw($@"select SUM(s.Netto) [AmountTransfer]
	                    , COUNT(e.EmployeeID) [CountEmployee]
	                    , CONVERT(decimal,SUM(s.Netto))/COUNT(e.EmployeeID) [AVGAmountEmployee]
                    from Salary s
                    inner join Employees e on e.EmployeeID=s.EmployeeID
                    where s.IsDeleted=0
	                    and s.Month={template.FirstOrDefault()?.Month}
	                    and s.Year={template.FirstOrDefault()?.Year}").ToListAsync();

                return new ListResponseUploadTemplate<SalaryDetailReportsDTO>(salaryDataList, salaryPayrollSummary, salaryPayrollSummaryTotal);
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

        public async Task<ListResponseTemplate<SalaryTemplateDTO>> GetSalaryTemplateAsync(string search, string sort, string filter)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var query = from employees in _context.Employees
                            where employees.IsDeleted == false
                            select new SalaryTemplateDTO
                            {
                                EmployeeID = employees.EmployeeID,
                                Nik = employees.Nik,
                                Name = employees.EmployeeName,
                                HKS = 23,
                                HKA = 0,
                                ATT = 0,
                                MEAL = 0,
                                Late = 0,
                                ABSENT = 0,
                                OVT = 0,
                                Rapel = 0,
                                OtherAllowances = 0,
                                OtherDeductions = 0
                            };

                int month = DateTime.Now.Month;
                int year = DateTime.Now.Year;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Contains(search)
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

                            if (fieldName == "month")
                                Int32.TryParse(value, out month);
                            else if (fieldName == "year")
                                Int32.TryParse(value, out year);

                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.Name.Contains(value)),
                                _ => query
                            };
                        }
                    }
                }
                query = query.Select(x => new SalaryTemplateDTO
                {
                    EmployeeID = x.EmployeeID,
                    Nik = x.Nik,
                    Name = x.Name,
                    HKS = x.HKS,
                    HKA = x.HKA,
                    ATT = x.ATT,
                    MEAL = x.MEAL,
                    Late = x.Late,
                    ABSENT = x.ABSENT,
                    OVT = x.OVT,
                    Rapel = x.Rapel,
                    OtherAllowances = x.OtherAllowances,
                    OtherDeductions = x.OtherDeductions,
                    Month = month,
                    Year = year
                });

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
                            "name" => query.OrderByDescending(x => x.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.Name),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.EmployeeID);
                }

                var result = await query.ToListAsync();
                return new ListResponseTemplate<SalaryTemplateDTO>(result); 
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<SalaryDetailReportsDTO>> GetGeneratePayrollResultAsync(string filter, long UserID)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var type = "";
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

                            if (fieldName == "type")
                                type = value;
                            else if (fieldName == "month")
                                Int32.TryParse(value, out month);
                            else if (fieldName == "year")
                                Int32.TryParse(value, out year);
                        }
                    }
                }

                var parameters = new List<SqlParameter>();
                parameters.Add(new SqlParameter("@Month", SqlDbType.Int) { Value = month });
                parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = year });
                parameters.Add(new SqlParameter("@IsFlag", SqlDbType.Int) { Value = 2 });
                parameters.Add(new SqlParameter("@UserID", SqlDbType.BigInt) { Value = UserID });

                var data = await _context.SalaryDetailReportsDTO.FromSqlRaw(
                  "EXEC usp_SalaryDetails @Month, @Year, @IsFlag, @UserID", parameters.ToArray())
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
        public async Task<ListResponseTemplate<SalaryPayrollBankDTO>> GetGenerateBankAsync(string filter)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                
                var type = "";
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

                            if (fieldName == "type")
                                type = value;
                            else if (fieldName == "month")
                                Int32.TryParse(value, out month);
                            else if (fieldName == "year")
                                Int32.TryParse(value, out year);
                        }
                    }
                }

                var query = (from salary in _context.Salary
                             join employee in _context.Employees on salary.EmployeeID equals employee.EmployeeID
                             join department in _context.Departments on employee.DepartmentID equals department.DepartmentID into departmentGroup
                             from department in departmentGroup.DefaultIfEmpty()
                             where salary.IsDeleted == false && salary.Month == month && salary.Year == year
                             select new SalaryPayrollBankDTO
                             {
                                 SalaryID = salary.SalaryID,
                                 EmployeeID = salary.EmployeeID,
                                 Nik = employee.Nik,
                                 Name = employee.EmployeeName,
                                 AccountNo = employee.AccountNo ?? "",
                                 Netto = salary.Netto,
                                 DepartmentCode = department.Code,
                                 TransDate = new DateTime(Convert.ToInt32(salary.Year), Convert.ToInt32(salary.Month), 1).AddMonths(1).AddDays(-1)
                             });

                // Sorting
                query = query.OrderByDescending(x => x.SalaryID);

                var resultList = await query.ToListAsync();

                return new ListResponseTemplate<SalaryPayrollBankDTO>(resultList);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
        public async Task<ListResponseTemplate<SalaryDetailReportsDTO>> GetEmployeeSalaryHistoryAsync(long EmployeeID, long Month, long Year, long UserID)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var data = await _context.SalaryDetailReportsDTO.FromSqlRaw($@"exec usp_SalaryDetailsByEmpID @EmployeeID,@Month,@Year, @UserID",
                    new SqlParameter("@EmployeeID", SqlDbType.BigInt) { Value = EmployeeID },
                    new SqlParameter("@Month", SqlDbType.BigInt) { Value = Month },
                    new SqlParameter("@Year", SqlDbType.BigInt) { Value = Year },
                    new SqlParameter("@UserID", SqlDbType.BigInt) { Value = UserID }).ToListAsync();

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

        public async Task<ListResponseTemplate<MasterEmployeePayroll>> GetMasterSalaryAsync(long EmployeeID)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var data = await _context.MasterEmployeePayroll.FromSqlRaw($@"exec usp_GetMasterSalaryByEmpID @EmployeeID",
                    new SqlParameter("@EmployeeID", SqlDbType.BigInt) { Value = EmployeeID }).ToListAsync();

                return new ListResponseTemplate<MasterEmployeePayroll>(data);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<bool> SetConfirmation(List<SalaryConfirmation> salaries, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var salaryIds = salaries.Select(x => x.SalaryID).ToList();
                var salaryList = await _context.Salary.Where(x => salaryIds.Contains(x.SalaryID) && x.IsDeleted == false).ToListAsync();
                if (salaryList == null || !salaryList.Any()) return false;

                var oldSalaryMonths = salaryList.Select(x => x.Month).Distinct().ToList();
                var oldSalaryYears = salaryList.Select(x => x.Year).Distinct().ToList();
                var oldSalary = await _context.Salary.Where(x => oldSalaryYears.Contains(x.Year) && oldSalaryMonths.Contains(x.Month) && x.IsDeleted == false).ToListAsync();

                oldSalary.ForEach(x => x.IsDeleted = true);

                salaryList.ForEach(x =>
                {
                    Int32.TryParse(x.Month.ToString(), out int month);
                    x.PayrollType = string.Concat("Monthly ", CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month), " ", x.Year);
                    x.UserUp = UserID;
                    x.DateUp = DateTime.Now;
                });
                
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
    }
}
