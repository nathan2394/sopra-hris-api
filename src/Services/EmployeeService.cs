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

namespace sopra_hris_api.src.Services.API
{
    public class EmployeeService : IServiceAsync<Employees>
    {
        private readonly EFContext _context;

        public EmployeeService(EFContext context)
        {
            _context = context;
        }

        public async Task<Employees> CreateAsync(Employees data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Employees.AddAsync(data);
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

        public async Task<Employees> EditAsync(Employees data)
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
                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                "nik" => query.Where(x => x.Nik.Contains(value)),
                                "ktp" => query.Where(x => x.KTP.Contains(value)),
                                "group" => query.Where(x => x.GroupID.ToString().Equals(value)),
                                "department" => query.Where(x => x.DepartmentID.ToString().Equals(value)),
                                "function" => query.Where(x => x.FunctionID.ToString().Equals(value)),
                                "division" => query.Where(x => x.DivisionID.ToString().Equals(value)),
                                "employeetype" => query.Where(x => x.EmployeeTypeID.ToString().Equals(value)),
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

                var details = await _context.AllowanceDeductionEmployeeDetails.FromSqlRaw($@"select a.AllowanceDeductionID,b.Name,b.Type,a.Amount 
from EmployeeDetails a
inner join AllowanceDeduction b on a.AllowanceDeductionID=b.AllowanceDeductionID
where EmployeeID={id}
union all
select a.AllowanceDeductionID,b.Name,b.Type,a.Amount 
from GroupDetails a
inner join AllowanceDeduction b on a.AllowanceDeductionID=b.AllowanceDeductionID
where GroupID={data.GroupID}
union all
select a.AllowanceDeductionID,b.Name,b.Type,a.Amount 
from FunctionDetails a
inner join AllowanceDeduction b on a.AllowanceDeductionID=b.AllowanceDeductionID
where FunctionID={data.FunctionID}").ToListAsync();
                data.allowancedeductionDetails = details;

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
