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
                var query = from a in _context.Salary where a.IsDeleted == false select a;

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

        public async Task<ListResponseResult<SalaryResultPayrollDTO>> GetSalaryResultPayrollAsync(List<SalaryTemplateDTO> template)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var salaryDataList = new List<SalaryResultPayrollDTO>();
                var bankSalaryDataList = new List<SalaryResultBankDTO>();
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
                        OtherAllowances = t.OtherAllowances,
                        OtherDeductions = t.OtherDeductions
                    };

                    await _context.SalaryHistory.AddAsync(salaryHistory);

                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Nik == t.Nik);

                    if (employee == null) continue;

                    var divisionID = await _context.Functions.Where(fd => fd.FunctionID == employee.FunctionID).Select(x => x.DivisionID).FirstOrDefaultAsync();

                    //basic salary based on attendance
                    var thp = ((t.HKA / t.HKS) * employee.BasicSalary);

                    var employeeDetails = await _context.EmployeeDetails.Where(x => x.EmployeeID == employee.EmployeeID).ToListAsync();
                    var groupDetails = await _context.GroupDetails.Where(x => x.GroupID == employee.GroupID).ToListAsync();
                    var functionDetails = await _context.FunctionDetails.Where(x => x.FunctionID == employee.FunctionID).ToListAsync();
                    var divisionDetails = await _context.DivisionDetails.Where(x => x.DivisionID == divisionID).ToListAsync();

                    // Process allowances and deductions using reusable method
                    (decimal? allowance, decimal? deduction) = await ProcessAllowancesAndDeductions(
                        employee.EmployeeID, employee.GroupID, employee.FunctionID, divisionID,
                        t.ATT,
                        employeeDetails,
                        groupDetails,
                        functionDetails,
                        divisionDetails,
                        dictAllowanceDeduction
                    );
                    totalAllowance += allowance;
                    totalDeduction += deduction;

                    //tunjangan kinerja
                    var yearsWorked = DateTime.Now.Year - employee.StartWorkingDate.Year;
                    var tunjanganKinerja = await _context.TunjanganKinerja
                        .Where(tk => yearsWorked >= tk.Min && yearsWorked <= tk.Max)
                        .Select(tk => tk.Factor)
                        .FirstOrDefaultAsync();

                    if (tunjanganKinerja > 0)
                    {
                        decimal amount = (tunjanganKinerja / 100 * employee.BasicSalary).Value;
                        totalAllowance += amount;
                        dictAllowanceDeduction.Add(8, amount);
                    }

                    var salaryNetto = thp + totalAllowance - totalDeduction + t.OtherAllowances - t.OtherDeductions;

                    // Check if there's an existing salary for this employee
                    var existingSalary = await _context.Salary
                        .Where(s => s.EmployeeID == t.EmployeeID && s.Month == t.Month && s.Year == t.Year)
                        .FirstOrDefaultAsync();

                    long salaryId = 0;
                    if (existingSalary != null)
                    {
                        salaryId = existingSalary.SalaryID;
                        existingSalary.HKA = t.HKA;
                        existingSalary.HKS = t.HKS;
                        existingSalary.ATT = t.ATT;
                        existingSalary.OVT = t.OVT;
                        existingSalary.Late = t.Late;
                        existingSalary.Netto = salaryNetto;
                        existingSalary.BasicSalary = employee.BasicSalary;
                        existingSalary.AllowanceTotal = totalAllowance;
                        existingSalary.DeductionTotal = totalDeduction;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
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
                            Netto = salaryNetto,
                            BasicSalary = employee.BasicSalary,
                            AllowanceTotal = totalAllowance,
                            DeductionTotal = totalDeduction
                        };
                        await _context.Salary.AddAsync(salary);
                        await _context.SaveChangesAsync();
                        salaryId = salary.SalaryID;
                    }

                    if (salaryId > 0)
                        await ProcessSalaryDetails(salaryId, dictAllowanceDeduction);

                    var employeeTypeName = await _context.EmployeeTypes.Where(x => x.EmployeeTypeID == employee.EmployeeTypeID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();
                    var employeeGroupName = await _context.Groups.Where(x => x.GroupID == employee.GroupID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();
                    var employeeFunctionName = await _context.Functions.Where(x => x.FunctionID == employee.FunctionID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();
                    var employeeDivisionName = await _context.Divisions.Where(x => x.DivisionID == divisionID && x.IsDeleted == false).Select(x => x.Name).FirstOrDefaultAsync();

                    // Prepare the DTO for payroll result
                    salaryDataList.Add(new SalaryResultPayrollDTO
                    {
                        EmployeeID = employee.EmployeeID,
                        Nik = employee.Nik ?? "",
                        Name = employee.EmployeeName ?? "",
                        EmployeeTypeID = employee.EmployeeTypeID,
                        EmployeeTypeName = employeeTypeName ?? "",
                        GroupID = employee.GroupID,
                        GroupName = employeeGroupName ?? "",
                        FunctionID = employee.FunctionID,
                        FunctionName = employeeFunctionName ?? "",
                        DivisionID = divisionID,
                        DivisionName = employeeDivisionName ?? "",
                        Month = t.Month,
                        Year = t.Year,
                        HKS = t.HKS ?? 0,
                        HKA = t.HKA ?? 0,
                        ATT = t.ATT ?? 0,
                        OVT = t.OVT ?? 0,
                        Late = t.Late ?? 0,
                        OtherAllowances = t.OtherAllowances ?? 0,
                        OtherDeductions = t.OtherDeductions ?? 0,
                        THP = salaryNetto,
                        //PayrollType = t.PayrollType ?? ""
                    });

                    // Prepare bank salary DTO
                    bankSalaryDataList.Add(new SalaryResultBankDTO
                    {
                        EmployeeID = employee.EmployeeID,
                        AccountNo = employee.AccountNo,
                        Bank = employee.Bank,
                        Nik = employee.Nik,
                        Name = employee.EmployeeName,
                        THP = salaryNetto
                    });
                }

                await dbTrans.CommitAsync();

                return new ListResponseResult<SalaryResultPayrollDTO>(salaryDataList, bankSalaryDataList);
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
                var existingDetail = await _context.SalaryDetails
                    .Where(sd => sd.SalaryID == salaryID && sd.AllowanceDeductionID == detail.Key)
                    .FirstOrDefaultAsync();

                if (existingDetail != null)  // If record exists, update it
                {
                    // Ensure the mapping exists in AllowanceDeduction
                    var allowanceDeduction = await _context.AllowanceDeduction
                        .FirstOrDefaultAsync(ad => ad.AllowanceDeductionID == detail.Key && ad.IsDeleted == false);

                    if (allowanceDeduction != null)
                        existingDetail.Amount = detail.Value;
                    else
                        // If no valid mapping, delete the record
                        _context.SalaryDetails.Remove(existingDetail);
                }
                else  // If the record does not exist, create a new one
                {
                    var salaryDetail = new SalaryDetails
                    {
                        AllowanceDeductionID = detail.Key,
                        Amount = detail.Value,
                        SalaryID = salaryID
                    };

                    await _context.SalaryDetails.AddAsync(salaryDetail);
                }
                await _context.SaveChangesAsync();
            }
        }

        // Reusable method to process allowances and deductions
        private async Task<(decimal? allowance, decimal? deduction)> ProcessAllowancesAndDeductions(
            long employeeID, long employeeGroupID, long employeeFunctionID, long employeeDivisionID, 
            decimal? ATT,
            List<EmployeeDetails> employeeDetails,
            List<GroupDetails> groupDetails,
            List<FunctionDetails> functionDetails,
            List<DivisionDetails> divisionDetails,
            Dictionary<long, decimal> dictAllowanceDeduction)
        {
            decimal allowance = 0;
            decimal deduction = 0;

            var allowanceDeductions = await _context.AllowanceDeduction.ToListAsync();
            // Process Employee-specific details (EmployeeDetails)
            foreach (var item in employeeDetails)
            {
                var allowanceDeduction = allowanceDeductions.FirstOrDefault(ad => ad.AllowanceDeductionID == item.AllowanceDeductionID);
                if (allowanceDeduction == null) continue;

                if (allowanceDeduction.Type == "Allowance")
                {
                    decimal amount = 0;
                    if (allowanceDeduction.AmountType == "Attendance" && item.Amount > 0 && ATT.HasValue && ATT.Value > 0)
                        amount = ATT.Value * item.Amount;  // Based on attendance
                    else if (allowanceDeduction.AmountType == "Fix")
                        amount = item.Amount;  // Fixed amount

                    dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    allowance += amount;
                }
                else if (allowanceDeduction.Type == "Deduction")
                {
                    decimal amount = item.Amount;
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
                    if (allowanceDeduction.AmountType == "Attendance" && item.Amount > 0 && ATT.HasValue && ATT.Value > 0)
                        amount = ATT.Value * item.Amount;  // Based on attendance
                    else if (allowanceDeduction.AmountType == "Fix")
                        amount = item.Amount;  // Fixed amount

                    dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    allowance += amount;
                }
                else if (allowanceDeduction.Type == "Deduction")
                {
                    decimal amount = item.Amount;
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
                    if (allowanceDeduction.AmountType == "Attendance" && item.Amount > 0 && ATT.HasValue && ATT.Value > 0)
                        amount = ATT.Value * item.Amount;  // Based on attendance
                    else if (allowanceDeduction.AmountType == "Fix")
                        amount = item.Amount;  // Fixed amount

                    dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    allowance += amount;
                }
                else if (allowanceDeduction.Type == "Deduction")
                {
                    decimal amount = item.Amount;
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
                    if (allowanceDeduction.AmountType == "Attendance" && item.Amount > 0 && ATT.HasValue && ATT.Value > 0)
                        amount = ATT.Value * item.Amount;  // Based on attendance
                    else if (allowanceDeduction.AmountType == "Fix")
                        amount = item.Amount;  // Fixed amount

                    dictAllowanceDeduction[item.AllowanceDeductionID] = amount;
                    allowance += amount;
                }
                else if (allowanceDeduction.Type == "Deduction")
                {
                    decimal amount = item.Amount;
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
                                Late = null,
                                OVT = null,
                                OtherAllowances = null,
                                OtherDeductions = null
                            };

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
    }
}
