using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using sopra_hris_api.src.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;

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

        public async Task<ListResponseTemplate<SalaryResultPayrollDTO>> GetSalaryResultPayrollAsync(List<SalaryTemplateDTO> template)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var month = template.FirstOrDefault()?.Month;
                var year = template.FirstOrDefault()?.Year;

                if (month != null && year != null)
                {
                    // Update existing Salary records where IsDeleted is 0 for the specified month, year, and employeeID
                    await _context.Salary
                        .Where(s => s.IsDeleted == false && s.Month == month && s.Year == year)
                        .ExecuteUpdateAsync(s => s.SetProperty(s => s.IsDeleted, true));
                }

                var salaryDataList = new List<SalaryResultPayrollDTO>();
                foreach (var t in template)
                {
                    decimal? totalAllowance = 0;
                    decimal? totalDeduction = 0;

                    var dictAllowanceDeduction = new Dictionary<long, decimal>();

                    var salaryHistory = new SalaryHistory
                    {
                        EmployeeID = t.EmployeeID,
                        NIK = t.Nik,
                        Month = t.Month,
                        Year = t.Year,
                        HKS = t.HKS,
                        HKA = t.HKA,
                        ATT = t.ATT,
                        OVT = t.OVT,
                        Late = t.Late,
                        MEAL = t.MEAL,
                        ABSENT = t.ABSENT,
                        OtherAllowances = t.OtherAllowances,
                        OtherDeductions = t.OtherDeductions
                    };

                    await _context.SalaryHistory.AddAsync(salaryHistory);

                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Nik == t.Nik);

                    if (employee == null) continue;

                    
                    //basic salary based on attendance
                    var thp = employee.BasicSalary * t.HKA / t.HKS; 

                    var employeeDetails = await _context.EmployeeDetails.Where(x => x.EmployeeID == employee.EmployeeID).ToListAsync();
                    var groupDetails = await _context.GroupDetails.Where(x => x.GroupID == employee.GroupID).ToListAsync();
                    var functionDetails = await _context.FunctionDetails.Where(x => x.FunctionID == employee.FunctionID).ToListAsync();
                    var divisionDetails = await _context.DivisionDetails.Where(x => x.DivisionID == employee.DivisionID).ToListAsync();

                    var yearsWorked = DateTime.Now.Year - employee.StartWorkingDate.Year;
                    // Process allowances and deductions using reusable method
                    (decimal? allowance, decimal? deduction) = await ProcessAllowancesAndDeductions(
                        t, employee,                        
                        employeeDetails, groupDetails, functionDetails,
                        divisionDetails, dictAllowanceDeduction
                    );
                    totalAllowance += allowance;
                    totalDeduction += deduction;

                    if (t?.OtherAllowances > 0)
                        totalAllowance += t.OtherAllowances;
                    if (t?.OtherDeductions > 0)
                        totalDeduction += t.OtherDeductions;

                    var salaryNetto = thp + totalAllowance - totalDeduction;
                    //var salaryNetto = employee.BasicSalary + totalAllowance - totalDeduction;

                    long salaryId = 0;
                    var salary = new Salary
                    {
                        EmployeeID = t.EmployeeID,
                        Month = t.Month,
                        Year = t.Year,
                        HKA = t.HKA,
                        HKS = t.HKS,
                        ATT = t.ATT,
                        OVT = t.OVT,
                        Late = t.Late,
                        MEAL = t.MEAL,
                        ABSENT = t.ABSENT,
                        Netto = salaryNetto,
                        BasicSalary = employee.BasicSalary,
                        AllowanceTotal = totalAllowance,
                        DeductionTotal = totalDeduction
                    };
                    await _context.Salary.AddAsync(salary);
                    await _context.SaveChangesAsync();
                    salaryId = salary.SalaryID;

                    if (salaryId > 0)
                        await ProcessSalaryDetails(salaryId, dictAllowanceDeduction);

                    var employeeTypeName = await _context.EmployeeTypes.Where(x => x.EmployeeTypeID == employee.EmployeeTypeID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();
                    var employeeGroupName = await _context.Groups.Where(x => x.GroupID == employee.GroupID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();
                    var employeeFunctionName = await _context.Functions.Where(x => x.FunctionID == employee.FunctionID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();
                    var employeeDepartmentName = await _context.Departments.Where(x => x.DepartmentID == employee.DepartmentID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();
                    var employeeDivisionName = await _context.Divisions.Where(x => x.DivisionID == employee.DivisionID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();
                    // Prepare the DTO for payroll result

                    //bpjs amount
                    dictAllowanceDeduction.TryGetValue(7L, out var bpjs);

                    salaryDataList.Add(new SalaryResultPayrollDTO
                    {
                        EmployeeID = employee.EmployeeID,
                        Nik = employee.Nik ?? "",
                        Name = employee.EmployeeName ?? "",
                        EmployeeTypeID = employee.EmployeeTypeID,
                        EmployeeTypeName = employeeTypeName ?? "",
                        GroupID = employee.GroupID,
                        GroupName = employeeGroupName ?? "",
                        FunctionID = employee.FunctionID ?? 0L,
                        FunctionName = employeeFunctionName ?? "",
                        DivisionID = employee.DivisionID ?? 0L,
                        DivisionName = employeeDivisionName ?? "",
                        DepartmentID = employee.DepartmentID ?? 0L,
                        DepartmentName = employeeDepartmentName ?? "",
                        Month = t.Month,
                        Year = t.Year,
                        HKS = t.HKS ?? 0,
                        HKA = t.HKA ?? 0,
                        ATT = t.ATT ?? 0,
                        MEAL = t.MEAL ?? 0,
                        OVT = t.OVT ?? 0,
                        Late = t.Late ?? 0,
                        ABSENT = t.ABSENT ?? 0,
                        TotalAllowances = totalAllowance ?? 0,
                        TotalDeductions = totalDeduction ?? 0,
                        THP = salaryNetto,
                        BPJS = bpjs,
                        TransferAmount = salaryNetto - bpjs
                    });
                }

                await dbTrans.CommitAsync();

                return new ListResponseTemplate<SalaryResultPayrollDTO>(salaryDataList);
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
        private async Task ProcessSalaryDetails(long salaryID, Dictionary<long, decimal> allowanceDeductionDetails)
        {
            foreach (var detail in allowanceDeductionDetails)
            {
                var salaryDetail = new SalaryDetails
                {
                    AllowanceDeductionID = detail.Key,
                    Amount = detail.Value,
                    SalaryID = salaryID
                };

                await _context.SalaryDetails.AddAsync(salaryDetail);
                await _context.SaveChangesAsync();
            }
        }
        
        // Reusable method to process allowances and deductions
        private async Task<(decimal? allowance, decimal? deduction)> ProcessAllowancesAndDeductions(
            SalaryTemplateDTO template, Employees employee,
            List<EmployeeDetails> employeeDetails,
            List<GroupDetails> groupDetails,
            List<FunctionDetails> functionDetails,
            List<DivisionDetails> divisionDetails,
            Dictionary<long, decimal> dictAllowanceDeduction)
        {
            // Calculate the difference in months
            int monthsDifference = (DateTime.Now.Year - employee.StartWorkingDate.Year) * 12 + DateTime.Now.Month - employee.StartWorkingDate.Month;

            // Calculate the difference in years (rounded down)
            int yearsWorked = monthsDifference / 12;

            decimal allowance = 0;
            decimal deduction = 0;

            var allowanceDeductions = await _context.AllowanceDeduction.ToListAsync();

            //tunjangan Masa Kerja
            var tunjanganMasaKerja = await _context.TunjanganMasaKerja
                .Where(tk => yearsWorked >= tk.Min && yearsWorked < tk.Max)
                .Select(tk => tk.Factor).FirstOrDefaultAsync();

            // Process Employee-specific details (EmployeeDetails)
            foreach (var item in employeeDetails)
            {
                var allowanceDeduction = allowanceDeductions.FirstOrDefault(ad => ad.AllowanceDeductionID == item.AllowanceDeductionID);
                if (allowanceDeduction == null) continue;

                if (allowanceDeduction.Type == "Allowance")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "MEAL" && item.Amount > 0 && template.MEAL.HasValue && template.MEAL.Value > 0)
                        amount = template.MEAL.Value * item.Amount;  // Based on meal
                    else if (allowanceDeduction.AmountType == "ATT" && item.Amount > 0 && template.ATT.HasValue && template.ATT.Value > 0)
                        amount = template.ATT.Value * item.Amount;  // Based on attendance
                    else if (allowanceDeduction.AmountType == "FIX" && item.Amount > 0)
                        amount = item.Amount;  // Fixed amount
                    else if (allowanceDeduction.AmountType == "CUSTOM" && tunjanganMasaKerja > 0)
                        amount = (employee.BasicSalary * tunjanganMasaKerja / 100).Value;
                    else if (allowanceDeduction.AmountType == "OVT" && item.Amount > 0 && template.OVT.HasValue && template.OVT.Value > 0)
                        amount = template.OVT.Value * (employee.BasicSalary / 173).Value;

                    if (!dictAllowanceDeduction.ContainsKey(item.AllowanceDeductionID))
                        dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    allowance += amount;
                }
                else if (allowanceDeduction.Type == "Deduction")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "FIX")
                        amount = item.Amount;
                    else if (allowanceDeduction.AmountType == "ABSENT" && template.ABSENT.HasValue && template.ABSENT.Value > 0)
                        amount = template.ABSENT.Value * (employee.BasicSalary / 173).Value;
                    else if (allowanceDeduction.AmountType == "LATE" && item.Amount > 0 && template.Late.HasValue && template.Late.Value > 0)
                        amount = template.Late.Value * item.Amount;

                    if (!dictAllowanceDeduction.ContainsKey(item.AllowanceDeductionID))
                        dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    deduction += amount;
                }
            }

            // Process Group-specific details (GroupDetails)
            foreach (var item in groupDetails)
            {
                var allowanceDeduction = allowanceDeductions.FirstOrDefault(ad => ad.AllowanceDeductionID == item.AllowanceDeductionID);
                if (allowanceDeduction == null) continue;

                if (allowanceDeduction.Type == "Allowance")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "MEAL" && item.Amount > 0 && template.MEAL.HasValue && template.MEAL.Value > 0)
                        amount = template.MEAL.Value * item.Amount;  // Based on meal
                    else if (allowanceDeduction.AmountType == "ATT" && item.Amount > 0 && template.ATT.HasValue && template.ATT.Value > 0)
                        amount = template.ATT.Value * item.Amount;  // Based on attendance
                    else if (allowanceDeduction.AmountType == "FIX" && item.Amount > 0)
                        amount = item.Amount;  // Fixed amount
                    else if (allowanceDeduction.AmountType == "CUSTOM" && tunjanganMasaKerja > 0)
                        amount = (tunjanganMasaKerja / 100 * employee.BasicSalary).Value;
                    else if (allowanceDeduction.AmountType == "OVT" && item.Amount > 0 && template.OVT.HasValue && template.OVT.Value > 0)
                        amount = template.OVT.Value * (employee.BasicSalary / 173).Value;

                    if (!dictAllowanceDeduction.ContainsKey(item.AllowanceDeductionID))
                        dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    allowance += amount;
                }
                else if (allowanceDeduction.Type == "Deduction")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "FIX")
                        amount = item.Amount;
                    else if (allowanceDeduction.AmountType == "ABSENT" && template.ABSENT.HasValue && template.ABSENT.Value > 0)
                        amount = template.ABSENT.Value * (employee.BasicSalary / 173).Value;
                    else if (allowanceDeduction.AmountType == "LATE" && item.Amount > 0 && template.Late.HasValue && template.Late.Value > 0)
                        amount = template.Late.Value * item.Amount;

                    if (!dictAllowanceDeduction.ContainsKey(item.AllowanceDeductionID))
                        dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    deduction += amount;
                }
            }

            // Process Function-specific details (FunctionDetails)
            foreach (var item in functionDetails)
            {
                var allowanceDeduction = allowanceDeductions.FirstOrDefault(ad => ad.AllowanceDeductionID == item.AllowanceDeductionID);
                if (allowanceDeduction == null) continue;

                if (allowanceDeduction.Type == "Allowance")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "MEAL" && item.Amount > 0 && template.MEAL.HasValue && template.MEAL.Value > 0)
                        amount = template.MEAL.Value * item.Amount;  // Based on meal
                    else if (allowanceDeduction.AmountType == "ATT" && item.Amount > 0 && template.ATT.HasValue && template.ATT.Value > 0)
                        amount = template.ATT.Value * item.Amount;  // Based on attendance
                    else if (allowanceDeduction.AmountType == "FIX" && item.Amount > 0)
                        amount = item.Amount;  // Fixed amount
                    else if (allowanceDeduction.AmountType == "CUSTOM" && tunjanganMasaKerja > 0)
                        amount = (tunjanganMasaKerja / 100 * employee.BasicSalary).Value;
                    else if (allowanceDeduction.AmountType == "OVT" && item.Amount > 0 && template.OVT.HasValue && template.OVT.Value > 0)
                        amount = template.OVT.Value * (employee.BasicSalary / 173).Value;

                    if (!dictAllowanceDeduction.ContainsKey(item.AllowanceDeductionID))
                        dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    allowance += amount;
                }
                else if (allowanceDeduction.Type == "Deduction")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "FIX")
                        amount = item.Amount;
                    else if (allowanceDeduction.AmountType == "ABSENT" && template.ABSENT.HasValue && template.ABSENT.Value > 0)
                        amount = template.ABSENT.Value * (employee.BasicSalary / 173).Value;
                    else if (allowanceDeduction.AmountType == "LATE" && item.Amount > 0 && template.Late.HasValue && template.Late.Value > 0)
                        amount = template.Late.Value * item.Amount;

                    if (!dictAllowanceDeduction.ContainsKey(item.AllowanceDeductionID))
                        dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    deduction += amount;
                }
            }

            // Process Division-specific details (DivisionDetails)
            foreach (var item in divisionDetails)
            {
                var allowanceDeduction = allowanceDeductions.FirstOrDefault(ad => ad.AllowanceDeductionID == item.AllowanceDeductionID);
                if (allowanceDeduction == null) continue;

                if (allowanceDeduction.Type == "Allowance")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "MEAL" && item.Amount > 0 && template.MEAL.HasValue && template.MEAL.Value > 0)
                        amount = template.MEAL.Value * item.Amount;  // Based on meal
                    else if (allowanceDeduction.AmountType == "ATT" && item.Amount > 0 && template.ATT.HasValue && template.ATT.Value > 0)
                        amount = template.ATT.Value * item.Amount;  // Based on attendance
                    else if (allowanceDeduction.AmountType == "FIX" && item.Amount > 0)
                        amount = item.Amount;  // Fixed amount
                    else if (allowanceDeduction.AmountType == "CUSTOM" && tunjanganMasaKerja > 0)
                        amount = (tunjanganMasaKerja / 100 * employee.BasicSalary).Value;
                    else if (allowanceDeduction.AmountType == "OVT" && item.Amount > 0 && template.OVT.HasValue && template.OVT.Value > 0)
                        amount = template.OVT.Value * (employee.BasicSalary / 173).Value;

                    if (!dictAllowanceDeduction.ContainsKey(item.AllowanceDeductionID))
                        dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    allowance += amount;
                }
                else if (allowanceDeduction.Type == "Deduction")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "FIX")
                        amount = item.Amount;
                    else if (allowanceDeduction.AmountType == "ABSENT" && template.ABSENT.HasValue && template.ABSENT.Value > 0)
                        amount = template.ABSENT.Value * (employee.BasicSalary / 173).Value;
                    else if (allowanceDeduction.AmountType == "LATE" && item.Amount > 0 && template.Late.HasValue && template.Late.Value > 0)
                        amount = template.Late.Value * item.Amount;

                    if (!dictAllowanceDeduction.ContainsKey(item.AllowanceDeductionID))
                        dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    deduction += amount;
                }
            }

            return (allowance, deduction);
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
                                HKS = null,
                                HKA = null,
                                ATT = null,
                                MEAL = null,
                                Late = null,
                                ABSENT = null,
                                OVT = null,
                                OtherAllowances = null,
                                OtherDeductions = null
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
        public async Task<ListResponseTemplate<object>> GetGenerateDataAsync(string search, string sort, string filter = "type:payroll")
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                
                var query = _context.Salary.Where(salary => salary.IsDeleted == false).AsQueryable();
                
                var type = "";
                var dateBetween = "";
                // Searching
                //if (!string.IsNullOrEmpty(search))
                //    query = query.Where(x => x.Name.Contains(search)
                //        );

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

                            //query = fieldName switch
                            //{
                            //    "name" => query.Where(x => x.Name.Contains(value)),
                            //    _ => query
                            //};
                        }
                    }
                }

                if (dateBetween != "")
                {
                    var dateSplit = dateBetween.Split("&", StringSplitOptions.RemoveEmptyEntries);
                    var start = Convert.ToDateTime(dateSplit[0].Trim());
                    var end = Convert.ToDateTime(dateSplit[1].Trim());
                    query = query.Where(x => x.Month == end.Month && x.Year == end.Year);

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
                    query = query.OrderByDescending(x => x.SalaryID);
                }

                if (!string.IsNullOrEmpty(type) && type.ToLower() == "bank")
                {
                    query = (from salary in _context.Salary
                             join employee in _context.Employees on salary.EmployeeID equals employee.EmployeeID
                             where salary.IsDeleted == false
                             select new Salary
                             {
                                 SalaryID = salary.SalaryID,
                                 EmployeeID = salary.EmployeeID,
                                 Nik = employee.Nik,
                                 Name = employee.EmployeeName,
                                 AccountNo = employee.AccountNo,
                                 Bank = employee.Bank,
                                 Netto = salary.Netto,
                                 TransDate = new DateTime(Convert.ToInt32(salary.Year), Convert.ToInt32(salary.Month), 1).AddMonths(1).AddDays(-1)
                             });
                }
                else if (!string.IsNullOrEmpty(type) && type.ToLower() == "payroll")
                {
                    query = from salary in _context.Salary
                            join employee in _context.Employees on salary.EmployeeID equals employee.EmployeeID into employeeGroup
                            from employee in employeeGroup.DefaultIfEmpty()
                            join employeeType in _context.EmployeeTypes on employee.EmployeeTypeID equals employeeType.EmployeeTypeID into employeeTypeGroup
                            from employeeType in employeeTypeGroup.DefaultIfEmpty()
                            join groups in _context.Groups on employee.GroupID equals groups.GroupID into groupsGroup
                            from groups in groupsGroup.DefaultIfEmpty()
                            join function in _context.Functions on employee.FunctionID equals function.FunctionID into functionGroup
                            from function in functionGroup.DefaultIfEmpty()
                            join department in _context.Departments on employee.DepartmentID equals department.DepartmentID into departmentGroup
                            from department in departmentGroup.DefaultIfEmpty()
                            join division in _context.Divisions on employee.DivisionID equals division.DivisionID into divisionGroup
                            from division in divisionGroup.DefaultIfEmpty()
                            where salary.IsDeleted == false

                            let bpjsAmount = _context.SalaryDetails
                                                  .Where(sd => sd.SalaryID == salary.SalaryID && sd.AllowanceDeductionID == 7)
                                                  .Sum(sd => sd.Amount)
                            select new Salary
                            {
                                SalaryID = salary.SalaryID,
                                EmployeeID = salary.EmployeeID,
                                Nik = employee != null ? employee.Nik : null,
                                Name = employee != null ? employee.EmployeeName : null,
                                EmployeeTypeID = employee != null ? employee.EmployeeTypeID : 0L,
                                EmployeeTypeName = employeeType != null ? employeeType.Name : null,
                                GroupID = groups != null ? groups.GroupID : 0L,
                                GroupType = groups != null ? groups.Type : null,
                                GroupName = groups != null ? groups.Name : null,
                                FunctionID = function != null ? function.FunctionID : 0L,
                                FunctionName = function != null ? function.Name : null,
                                DivisionID = division != null ? division.DivisionID : 0L,
                                DivisionName = division != null ? division.Name : null,
                                DepartmentID = department != null ? department.DepartmentID : 0L,
                                DepartmentName = department != null ? department.Name : null,
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
                                PayrollType = salary.PayrollType,
                                BPJS = bpjsAmount,
                                TransferAmount = salary.Netto - bpjsAmount
                            };
                }

                var resultList = await query.ToListAsync();

                return new ListResponseTemplate<object>(resultList);
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
