using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.Design;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Data;

namespace sopra_hris_api.src.Services.API
{
    public class EmployeeService : IServiceEmployeeAsync<Employees>
    {
        private readonly EFContext _context;

        public EmployeeService(EFContext context)
        {
            _context = context;
        }

        public async Task<Employees> CreateAsync(Employees data, List<EmployeeDetails> dataDetails, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.UserIn = userID;
                await _context.Employees.AddAsync(data);
                await _context.SaveChangesAsync();

                if (data.EmployeeID > 0)
                {
                    dataDetails.ForEach(detail =>
                    {
                        detail.EmployeeID = data.EmployeeID; 
                        detail.UserIn = userID;
                    });

                    await _context.EmployeeDetails.AddRangeAsync(dataDetails);
                    await _context.SaveChangesAsync();
                }

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
                var obj = await _context.Employees.FirstOrDefaultAsync(x => x.EmployeeID == id && x.IsDeleted == false);
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

        public async Task<Employees> EditAsync(Employees data, List<EmployeeDetails> details, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Employees.FirstOrDefaultAsync(x => x.EmployeeID == data.EmployeeID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeName = data.EmployeeName;
                obj.NickName = data.NickName;
                obj.Nik = data.Nik;
                obj.PlaceOfBirth = data.PlaceOfBirth;
                obj.DateOfBirth = data.DateOfBirth;
                obj.Gender = data.Gender;
                obj.Email = data.Email;
                obj.PhoneNumber = data.PhoneNumber;
                obj.KTP = data.KTP;
                obj.StartWorkingDate = data.StartWorkingDate;
                obj.StartJointDate = data.StartJointDate;
                obj.EndWorkingDate = data.EndWorkingDate;
                obj.EmployeeTypeID = data.EmployeeTypeID;
                obj.GroupID = data.GroupID;
                obj.DepartmentID = data.DepartmentID;
                obj.DivisionID = data.DivisionID;
                obj.FunctionID = data.FunctionID;
                obj.JobTitleID = data.JobTitleID;
                obj.Religion = data.Religion;
                obj.BPJSTK = data.BPJSTK;
                obj.BPJSKES = data.BPJSKES;
                obj.Education = data.Education;
                obj.TaxStatus = data.TaxStatus;
                obj.MotherMaidenName = data.MotherMaidenName;
                obj.TKStatus = data.TKStatus;
                obj.BPJSKES = data.BPJSKES;
                obj.BPJSKES = data.BPJSKES;
                obj.Bank = data.Bank;
                obj.AccountNo = data.AccountNo;
                obj.Bank = data.Bank;
                obj.AddressKTP = data.AddressKTP;
                obj.AddressDomisili = data.AddressDomisili;
                obj.BasicSalary = data.BasicSalary;
                obj.CompanyID = data.CompanyID;

                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                if (obj.EmployeeID > 0)
                {
                    // Fetch existing EmployeeDetails for this employee
                    var existingDetails = await _context.EmployeeDetails
                        .Where(ed => ed.EmployeeID == obj.EmployeeID)
                        .ToListAsync();

                    // Create or update EmployeeDetails records
                    var newDetails = details ?? new List<EmployeeDetails>();

                    // Find records to delete (those that are in the database but not in the new data)
                    var toDelete = existingDetails
                        .Where(ed => !newDetails.Any(nd => nd.AllowanceDeductionID == ed.AllowanceDeductionID))
                        .ToList();

                    // Process new or updated EmployeeDetails
                    foreach (var newDetail in newDetails)
                    {
                        var existingDetail = existingDetails
                            .FirstOrDefault(ed => ed.AllowanceDeductionID == newDetail.AllowanceDeductionID);

                        if (existingDetail != null)
                        {
                            // Update existing record's Amount
                            existingDetail.Amount = newDetail.Amount;
                            existingDetail.DateUp = DateTime.Now;
                            existingDetail.UserUp = userID;
                            _context.EmployeeDetails.Update(existingDetail);
                        }
                        else
                        {
                            // Insert new record if it doesn't exist
                            newDetail.EmployeeID = obj.EmployeeID; 
                            await _context.EmployeeDetails.AddAsync(newDetail);
                        }
                    }

                    // Delete any EmployeeDetails records that are no longer part of the new data
                    if (toDelete.Any())
                    {
                        foreach (var objDelete in toDelete)
                        {
                            objDelete.IsDeleted = true;
                            objDelete.DateUp = DateTime.Now;
                            objDelete.UserUp = userID;
                            _context.EmployeeDetails.Update(objDelete);
                        }
                    }

                    // Save changes to EmployeeDetails
                    await _context.SaveChangesAsync();
                }
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


        public async Task<ListResponse<Employees>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Employees
                            join employeeType in _context.EmployeeTypes
                                on a.EmployeeTypeID equals employeeType.EmployeeTypeID into employeeTypeGroup
                            from employeeType in employeeTypeGroup.DefaultIfEmpty()

                            join groups in _context.Groups
                                on a.GroupID equals groups.GroupID into groupGroup
                            from groups in groupGroup.DefaultIfEmpty()

                            join function in _context.Functions
                                on a.FunctionID equals function.FunctionID into functionGroup
                            from function in functionGroup.DefaultIfEmpty()

                            join division in _context.Divisions
                                on a.DivisionID equals division.DivisionID into divisionGroup
                            from division in divisionGroup.DefaultIfEmpty()

                            join department in _context.Departments
                                on a.DepartmentID equals department.DepartmentID into departmentGroup
                            from department in departmentGroup.DefaultIfEmpty()

                            join jobtitle in _context.EmployeeJobTitles
                                on a.JobTitleID equals jobtitle.EmployeeJobTitleID into jobTitleGroup
                            from jobtitle in jobTitleGroup.DefaultIfEmpty()

                            where a.IsDeleted == false
                            select new Employees
                            {
                                EmployeeID = a.EmployeeID,
                                Nik = a.Nik,
                                EmployeeName = a.EmployeeName,
                                NickName = a.NickName,
                                PlaceOfBirth = a.PlaceOfBirth,
                                DateOfBirth = a.DateOfBirth,
                                Gender = a.Gender,
                                Email = a.Email,
                                PhoneNumber = a.PhoneNumber,
                                KTP = a.KTP,
                                StartWorkingDate = a.StartWorkingDate,
                                StartJointDate = a.StartJointDate,
                                EndWorkingDate = a.EndWorkingDate,
                                EmployeeTypeID = a.EmployeeTypeID,
                                EmployeeTypeName = employeeType != null ? employeeType.Name : null,
                                GroupID = a.GroupID,
                                GroupName = groups != null ? groups.Name : null,
                                GroupType = groups != null ? groups.Type : null,
                                FunctionID = a.FunctionID,
                                FunctionName = function != null ? function.Name : null,
                                DivisionID = a.DivisionID,
                                DivisionName = division != null ? division.Name : null,
                                DepartmentID = a.DepartmentID,
                                DepartmentName = department != null ? department.Name : null,
                                JobTitleID = a.EmployeeTypeID,
                                EmployeeJobTitleName = jobtitle != null ? jobtitle.Name : null,
                                AccountNo = a.AccountNo,
                                Bank = a.Bank,
                                CompanyID = a.CompanyID,
                                AddressKTP = a.AddressKTP,
                                AddressDomisili = a.AddressDomisili,
                                BasicSalary = a.BasicSalary,
                                Religion = a.Religion,
                                BPJSTK = a.BPJSTK,
                                BPJSKES = a.BPJSKES,
                                Education = a.Education,
                                TaxStatus = a.TaxStatus,
                                MotherMaidenName = a.MotherMaidenName,
                                TKStatus = a.TKStatus
                            };
                // Searching

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeeName.Contains(search)
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

                            if (fieldName == "group" || fieldName == "department" || fieldName == "function" || fieldName == "employeetype" || fieldName == "division")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "group")
                                    query = query.Where(x => Ids.Contains(x.GroupID));
                                else if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                                else if (fieldName == "function")
                                    query = query.Where(x => Ids.Contains(x.FunctionID ?? 0));
                                else if (fieldName == "employeetype")
                                    query = query.Where(x => Ids.Contains(x.EmployeeTypeID));
                                else if (fieldName == "division")
                                    query = query.Where(x => Ids.Contains(x.DivisionID ?? 0));
                            }
                            else
                            {
                                query = fieldName switch
                                {
                                    "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                    "nik" => query.Where(x => x.Nik.Contains(value)),
                                    "ktp" => query.Where(x => x.KTP.Contains(value)),
                                    "department" => query.Where(x => x.DepartmentID.ToString().Equals(value)),
                                    "function" => query.Where(x => x.FunctionID.ToString().Equals(value)),
                                    "division" => query.Where(x => x.DivisionID.ToString().Equals(value)),
                                    "employeetype" => query.Where(x => x.EmployeeTypeID.ToString().Equals(value)),
                                    _ => query
                                };
                            }
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
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.EmployeeName),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.EmployeeID);
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

                return new ListResponse<Employees>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Employees> GetByIdAsync(long id)
        {
            try
            {
                var query = from a in _context.Employees
                            join employeeType in _context.EmployeeTypes
                                on a.EmployeeTypeID equals employeeType.EmployeeTypeID into employeeTypeGroup
                            from employeeType in employeeTypeGroup.DefaultIfEmpty()

                            join groups in _context.Groups
                                on a.GroupID equals groups.GroupID into groupGroup
                            from groups in groupGroup.DefaultIfEmpty()

                            join function in _context.Functions
                                on a.FunctionID equals function.FunctionID into functionGroup
                            from function in functionGroup.DefaultIfEmpty()

                            join division in _context.Divisions
                                on a.DivisionID equals division.DivisionID into divisionGroup
                            from division in divisionGroup.DefaultIfEmpty()

                            join department in _context.Departments
                                on a.DepartmentID equals department.DepartmentID into departmentGroup
                            from department in departmentGroup.DefaultIfEmpty()

                            join jobtitle in _context.EmployeeJobTitles
                                on a.JobTitleID equals jobtitle.EmployeeJobTitleID into jobTitleGroup
                            from jobtitle in jobTitleGroup.DefaultIfEmpty()

                            where a.IsDeleted == false && a.EmployeeID == id
                            select new Employees
                            {
                                EmployeeID = a.EmployeeID,
                                Nik = a.Nik,
                                EmployeeName = a.EmployeeName,
                                NickName = a.NickName,
                                PlaceOfBirth = a.PlaceOfBirth,
                                DateOfBirth = a.DateOfBirth,
                                Gender = a.Gender,
                                Email = a.Email,
                                PhoneNumber = a.PhoneNumber,
                                KTP = a.KTP,
                                StartWorkingDate = a.StartWorkingDate,
                                StartJointDate = a.StartJointDate,
                                EndWorkingDate = a.EndWorkingDate,
                                EmployeeTypeID = a.EmployeeTypeID,
                                EmployeeTypeName = employeeType != null ? employeeType.Name : null,
                                GroupID = a.GroupID,
                                GroupName = groups != null ? groups.Name : null,
                                GroupType = groups != null ? groups.Type : null,
                                FunctionID = a.FunctionID,
                                FunctionName = function != null ? function.Name : null,
                                DivisionID = a.DivisionID,
                                DivisionName = division != null ? division.Name : null,
                                DepartmentID = a.DepartmentID,
                                DepartmentName = department != null ? department.Name : null,
                                JobTitleID = a.EmployeeTypeID,
                                EmployeeJobTitleName = jobtitle != null ? jobtitle.Name : null,
                                AccountNo = a.AccountNo,
                                Bank = a.Bank,
                                CompanyID = a.CompanyID,
                                AddressKTP = a.AddressKTP,
                                AddressDomisili = a.AddressDomisili,
                                BasicSalary = a.BasicSalary,
                                Religion = a.Religion,
                                BPJSTK = a.BPJSTK,
                                BPJSKES = a.BPJSKES,
                                Education = a.Education,
                                TaxStatus = a.TaxStatus,
                                MotherMaidenName = a.MotherMaidenName,
                                TKStatus = a.TKStatus
                            };
                var data = await query.AsNoTracking().FirstOrDefaultAsync();

                var details = await _context.AllowanceDeductionEmployeeDetails.FromSqlRaw($@"select EmployeeDetailID ID,'Employee' AllowanceDeductionGroupType, a.AllowanceDeductionID, b.Name, b.Type, a.Amount 
from EmployeeDetails a
inner join AllowanceDeduction b on a.AllowanceDeductionID = b.AllowanceDeductionID
where a.EmployeeID = {id}
union all
select GroupDetailID ID, 'Grade' AllowanceDeductionGroupType, a.AllowanceDeductionID, b.Name, b.Type, a.Amount 
from GroupDetails a
inner join AllowanceDeduction b on a.AllowanceDeductionID = b.AllowanceDeductionID
left join EmployeeDetails ed on ed.AllowanceDeductionID = a.AllowanceDeductionID and ed.EmployeeID = {id}
where a.GroupID = {data.GroupID}
and ed.AllowanceDeductionID is null").ToListAsync();

                var masterSalary = await _context.MasterEmployeePayroll.FromSqlRaw($@"exec usp_GetMasterSalaryByEmpID @EmployeeID",
                    new SqlParameter("@EmployeeID", SqlDbType.BigInt) { Value = id }).ToListAsync();

                var salaryHistories = await _context.Salary.Where(x => x.EmployeeID == id && x.IsDeleted == false
                && (x.PayrollType == null || !x.PayrollType.StartsWith("Master Data Payroll")))
                    .Select(x => new EmployeeSalaryHistory
                    {
                        SalaryID = x.SalaryID,
                        Month = x.Month,
                        Year = x.Year,
                        Netto = x.Netto
                    }).ToListAsync();
                data.AllowanceDeductionDetails = details;
                data.MasterEmployeePayroll = masterSalary;
                data.salaryHistories = salaryHistories;
                if (data == null) return null;
                return data;
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
