﻿using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.src.Entities;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Data;

namespace sopra_hris_api.src.Services.API
{
    public class SalaryService : IServiceSalaryAsync<Salary>
    {
        private readonly EFContext _context;

        public SalaryService(EFContext context)
        {
            _context = context;
        }

        public async Task<Salary> CreateAsync(Salary data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Salary.AddAsync(data);
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
                var obj = await _context.Salary.FirstOrDefaultAsync(x => x.SalaryID == id && x.IsDeleted == false);
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

        public async Task<Salary> EditAsync(Salary data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Salary.FirstOrDefaultAsync(x => x.SalaryID == data.SalaryID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.Month = data.Month;
                obj.Year = data.Year;
                obj.HKS = data.HKS;
                obj.HKA = data.HKA;
                obj.ATT = data.ATT;
                obj.Late = data.Late;
                obj.OVT = data.OVT;
                obj.AllowanceTotal = data.AllowanceTotal;
                obj.DeductionTotal = data.DeductionTotal;

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

        public async Task<Salary> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Salary.AsNoTracking().FirstOrDefaultAsync(x => x.SalaryID == id && x.IsDeleted == false);
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

                var parameters = new List<SqlParameter>();
                parameters.Add(new SqlParameter("@Month", SqlDbType.Int) { Value = template.FirstOrDefault()?.Month });
                parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = template.FirstOrDefault()?.Year });
                var salaryDataList = await _context.SalaryDetailReportsDTO.FromSqlRaw(
                  "EXEC usp_SalaryDetails @Month, @Year", parameters.ToArray())
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
                                OtherAllowances = 0,
                                OtherDeductions = 0
                            };

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
                                "name" => query.Where(x => x.Name.Contains(value)),
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
        public async Task<ListResponseTemplate<SalaryDetailReportsDTO>> GetGeneratePayrollResultAsync(string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var type = "";
                var dateBetween = "";
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

                            
                        }
                    }
                }

                if (dateBetween != "")
                {
                    var dateSplit = dateBetween.Split("&", StringSplitOptions.RemoveEmptyEntries);
                    var start = Convert.ToDateTime(dateSplit[0].Trim());
                    var end = Convert.ToDateTime(dateSplit[1].Trim());
                    month = end.Month;
                    year = end.Year;
                }

                var parameters = new List<SqlParameter>();
                parameters.Add(new SqlParameter("@Month", SqlDbType.Int) { Value = month });
                parameters.Add(new SqlParameter("@Year", SqlDbType.Int) { Value = year });

                var data = await _context.SalaryDetailReportsDTO.FromSqlRaw(
                  "EXEC usp_SalaryDetails @Month, @Year", parameters.ToArray())
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
        public async Task<ListResponseTemplate<SalaryPayrollBankDTO>> GetGenerateBankAsync(string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                
                var type = "";
                var dateBetween = "";
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
                        }
                    }
                }

                if (dateBetween != "")
                {
                    var dateSplit = dateBetween.Split("&", StringSplitOptions.RemoveEmptyEntries);
                    var start = Convert.ToDateTime(dateSplit[0].Trim());
                    var end = Convert.ToDateTime(dateSplit[1].Trim());
                    month = end.Month;
                    year = end.Year;
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

    }
}
