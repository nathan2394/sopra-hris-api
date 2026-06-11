using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using System.Data;
using System.Security.Claims;
using sopra_hris_api.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class EmployeeService : IServiceEmployeeAsync<Employees>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public EmployeeService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public async Task<Employees> CreateAsync(Employees data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var sequence = await _context.Employees.Where(x => x.StartWorkingDate.Month == data.StartWorkingDate.Month && x.StartWorkingDate.Year == data.StartWorkingDate.Year && x.IsDeleted == false).CountAsync();
                data.Nik = string.Concat(data.CompanyID, data.StartWorkingDate.ToString("yyyyMM"), (sequence + 1).ToString("D3"));
                
                await _context.Employees.AddAsync(data);
                await _context.SaveChangesAsync();

                var users = new Users
                {
                    Name = data.EmployeeName,
                    EmployeeID = data.EmployeeID,
                    Email = data.Email,
                    PhoneNumber = data.PhoneNumber,

                    IsDeleted = false,
                    UserIn = data.UserIn,
                    DateIn = DateTime.Now,
                };

                await _context.Users.AddAsync(users);
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

        public async Task<CreateEmployeeFromPortalResponse> CreateEmployeeAsync(
            CreateEmployeeFromPortalRequest data,
            long userId
        )
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();

            try
            {
                if (data == null)
                    throw new Exception("Payload is required");

                var ktp = string.IsNullOrWhiteSpace(data.KTP) ? null : data.KTP.Trim();
                var employeeName = string.IsNullOrWhiteSpace(data.EmployeeName)
                    ? null
                    : data.EmployeeName.Trim();
                var email = string.IsNullOrWhiteSpace(data.Email) ? null : data.Email.Trim();
                var phoneNumber = string.IsNullOrWhiteSpace(data.PhoneNumber)
                    ? null
                    : data.PhoneNumber.Trim();
                var employeeTypeName = string.IsNullOrWhiteSpace(data.EmployeeTypeName)
                    ? null
                    : data.EmployeeTypeName.Trim();
                var rawGender = string.IsNullOrWhiteSpace(data.Gender)
                    ? null
                    : data.Gender.Trim();
                var genderKey = (rawGender ?? string.Empty).ToUpperInvariant();
                var gender = genderKey switch
                {
                    "MALE" => "Pria",
                    "PRIA" => "Pria",
                    "LAKI-LAKI" => "Pria",
                    "LAKI LAKI" => "Pria",
                    "FEMALE" => "Wanita",
                    "WANITA" => "Wanita",
                    "PEREMPUAN" => "Wanita",
                    _ => null,
                };

                if (string.IsNullOrWhiteSpace(ktp))
                    throw new Exception("KTP is required");

                if (string.IsNullOrWhiteSpace(employeeName))
                    throw new Exception("Employee name is required");

                if (!data.DateOfBirth.HasValue)
                    throw new Exception("Date of birth is required");

                if (string.IsNullOrWhiteSpace(gender))
                    throw new Exception("Gender is required or invalid");

                if (string.IsNullOrWhiteSpace(email))
                    throw new Exception("Email is required");

                if (!data.StartWorkingDate.HasValue)
                    throw new Exception("Start working date is required");

                if (data.CompanyID <= 0)
                    throw new Exception("CompanyID is required");

                if (data.GroupID <= 0)
                    throw new Exception("GroupID is required");

                if (string.IsNullOrWhiteSpace(employeeTypeName))
                    throw new Exception("Employee type is required");

                var existingEmployee = await _context.Employees.FirstOrDefaultAsync(x =>
                    x.IsDeleted == false && x.KTP == ktp
                );

                if (existingEmployee != null)
                {
                    await dbTrans.CommitAsync();

                    return new CreateEmployeeFromPortalResponse
                    {
                        CandidateJobOfferID = data.CandidateJobOfferID,
                        CandidateID = data.CandidateID,
                        EmployeeID = existingEmployee.EmployeeID,
                        Nik = existingEmployee.Nik,
                        KTP = ktp,
                        Status = "Skipped",
                        Message = "Employee already exists.",
                    };
                }

                var normalizedEmployeeTypeName = employeeTypeName.ToUpperInvariant();
                var employeeType = await _context.EmployeeTypes.FirstOrDefaultAsync(x =>
                    x.IsDeleted == false
                    && x.Name != null
                    && x.Name.Trim().ToUpper() == normalizedEmployeeTypeName
                );

                if (employeeType == null)
                    throw new Exception("Employee type is not found");

                var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.IsDeleted == false && x.CompanyID == data.CompanyID
                );

                if (company == null)
                    throw new Exception("Company is not found");

                var group = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.IsDeleted == false && x.GroupID == data.GroupID
                );

                if (group == null)
                    throw new Exception("Group is not found");

                var startWorkingDate = data.StartWorkingDate.Value.Date;
                var companyCode = string.IsNullOrWhiteSpace(company.Code)
                    ? null
                    : company.Code.Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(companyCode))
                    throw new Exception("Company code is required");

                string nikPrefix;
                var nikRunningDigits = 3;

                if (companyCode == "SOPRA")
                {
                    nikPrefix = startWorkingDate.ToString("yyMM");
                    nikRunningDigits = 4;
                }
                else if (companyCode == "TRASS" || companyCode == "SOPRA_DEV")
                {
                    var yearPart = startWorkingDate.ToString("yyyy");

                    if (group.Level == 0 || group.Level == 1)
                        yearPart = "9" + yearPart.Substring(1);

                    nikPrefix = "2" + yearPart + startWorkingDate.ToString("MM");
                }
                else
                {
                    throw new Exception($"Company code {companyCode} is not supported");
                }

                var sequence = await _context.Employees.CountAsync(x =>
                    x.IsDeleted == false && x.Nik.StartsWith(nikPrefix)
                );
                var nik = nikPrefix + (sequence + 1).ToString($"D{nikRunningDigits}");

                var employee = new Employees
                {
                    Nik = nik,
                    EmployeeName = employeeName,
                    NickName = employeeName,
                    PlaceOfBirth = string.IsNullOrWhiteSpace(data.PlaceOfBirth)
                        ? null
                        : data.PlaceOfBirth.Trim(),
                    DateOfBirth = data.DateOfBirth.Value.Date,
                    Gender = gender,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    KTP = ktp,
                    NPWP = string.IsNullOrWhiteSpace(data.NPWP) ? null : data.NPWP.Trim(),
                    StartWorkingDate = startWorkingDate,
                    EmployeeTypeID = employeeType.EmployeeTypeID,
                    GroupID = data.GroupID,
                    DepartmentID = null,
                    DivisionID = null,
                    FunctionID = null,
                    JobTitleID = null,
                    Religion = string.IsNullOrWhiteSpace(data.Religion) ? null : data.Religion.Trim(),
                    BPJSTK = null,
                    BPJSKES = null,
                    Education = string.IsNullOrWhiteSpace(data.Education)
                        ? null
                        : data.Education.Trim(),
                    TaxStatus = null,
                    MotherMaidenName = null,
                    TKStatus = null,
                    CompanyID = data.CompanyID,
                    AddressKTP = null,
                    AddressDomisili = string.IsNullOrWhiteSpace(data.Address)
                        ? null
                        : data.Address.Trim(),
                    BasicSalary = data.BasicSalary,
                    AccountNo = string.IsNullOrWhiteSpace(data.AccountNo)
                        ? null
                        : data.AccountNo.Trim(),
                    Bank = string.IsNullOrWhiteSpace(data.Bank) ? null : data.Bank.Trim(),
                    PayrollType = employeeType.Name,
                    UserIn = userId,
                    DateIn = DateTime.Now,
                    IsDeleted = false,
                };

                await _context.Employees.AddAsync(employee);
                await _context.SaveChangesAsync();

                var user = new Users
                {
                    Name = employee.EmployeeName,
                    EmployeeID = employee.EmployeeID,
                    Email = employee.Email,
                    PhoneNumber = employee.PhoneNumber,
                    IsDeleted = false,
                    UserIn = userId,
                    DateIn = DateTime.Now,
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                await dbTrans.CommitAsync();

                return new CreateEmployeeFromPortalResponse
                {
                    CandidateJobOfferID = data.CandidateJobOfferID,
                    CandidateID = data.CandidateID,
                    EmployeeID = employee.EmployeeID,
                    UserID = user.UserID,
                    Nik = employee.Nik,
                    KTP = ktp,
                    Status = "Created",
                    Message = "Employee created successfully.",
                };
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
                obj.PayrollType = data.PayrollType;
                obj.AbsentID = data.AbsentID;
                obj.ShiftID = data.ShiftID;
                obj.IsShift = data.IsShift;
                obj.GroupShiftID = data.GroupShiftID;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;
                await _context.SaveChangesAsync();

                var users = await _context.Users.FirstOrDefaultAsync(x => x.EmployeeID == data.EmployeeID && x.IsDeleted == false);
                if (users != null)
                {
                    users.Name = data.EmployeeName;
                    users.Email = data.Email;
                    users.PhoneNumber = data.PhoneNumber;
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
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                var EmployeeID = Convert.ToInt64(User.FindFirstValue("employeeid"));
                var GroupID = Convert.ToInt64(User.FindFirstValue("groupid"));
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var groupLevel = await _context.Groups.Where(y => y.GroupID == GroupID).Select(x => x.Level).FirstOrDefaultAsync();

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

                            where a.IsDeleted == false && (groups.Level < groupLevel || groupLevel == 0)
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
                                JobTitleID = a.JobTitleID,
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
                                TKStatus = a.TKStatus,
                                PayrollType = a.PayrollType,
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
                                    "status" => query.Where(x => value == "nonactive" ? x.EndWorkingDate.HasValue : !x.EndWorkingDate.HasValue),
                                    "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                    "nik" => query.Where(x => x.Nik.Contains(value)),
                                    "ktp" => query.Where(x => x.KTP.Contains(value)),
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

                data = data
                    .Select(e =>
                    {
                        e.BasicSalary = Utility.MaskSalary(RoleID, e.BasicSalary ?? 0);
                        return e;
                    })
                    .ToList();
                
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
        public async Task<ListResponse<Employees>> GetList(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var UserID = Convert.ToInt64(User.FindFirstValue("id"));
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var query = from a in _context.Employees
                            join employeeType in _context.EmployeeTypes
                                on a.EmployeeTypeID equals employeeType.EmployeeTypeID into employeeTypeGroup
                            from employeeType in employeeTypeGroup.DefaultIfEmpty()

                            join groups in _context.Groups
                                on a.GroupID equals groups.GroupID into groupGroup
                            from groups in groupGroup.DefaultIfEmpty()

                            join division in _context.Divisions
                                on a.DivisionID equals division.DivisionID into divisionGroup
                            from division in divisionGroup.DefaultIfEmpty()

                            join department in _context.Departments
                                on a.DepartmentID equals department.DepartmentID into departmentGroup
                            from department in departmentGroup.DefaultIfEmpty()

                            where a.IsDeleted == false && a.EndWorkingDate == null
                            select new Employees
                            {
                                EmployeeID = a.EmployeeID,
                                Nik = a.Nik,
                                EmployeeName = a.EmployeeName,
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
                                DivisionID = a.DivisionID,
                                DivisionName = division != null ? division.Name : null,
                                DepartmentID = a.DepartmentID,
                                DepartmentName = department != null ? department.Name : null,
                                CompanyID = a.CompanyID,
                                PayrollType = a.PayrollType,
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
                                    "status" => query.Where(x => value == "nonactive" ? x.EndWorkingDate.HasValue : !x.EndWorkingDate.HasValue),
                                    "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                    "nik" => query.Where(x => x.Nik.Contains(value)),
                                    "ktp" => query.Where(x => x.KTP.Contains(value)),
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
                    return await GetList(limit, page, total, search, sort, filter, date);
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
                var RoleID = Convert.ToInt64(User.FindFirstValue("roleid"));
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

                            join shift in _context.Shifts
                                on a.ShiftID equals shift.ShiftID into shiftGroup
                            from shift in shiftGroup.DefaultIfEmpty()

                            join groupshift in _context.GroupShifts
                                on a.GroupShiftID equals groupshift.GroupShiftID into groupshiftGroup
                            from groupshift in groupshiftGroup.DefaultIfEmpty()

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
                                JobTitleID = a.JobTitleID,
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
                                TKStatus = a.TKStatus,
                                PayrollType = a.PayrollType,
                                GroupShiftID = a.GroupShiftID,
                                AbsentID = a.AbsentID,
                                ShiftID = a.ShiftID,
                                IsShift = a.IsShift,
                                ShiftCode = shift != null ? shift.Code : null,
                                ShiftName = shift != null ? shift.Name : null,
                                GroupShiftCode = groupshift != null ? groupshift.Code : null,
                                GroupShiftName = groupshift != null ? groupshift.Name : null,
                            };
                var data = await query.AsNoTracking().FirstOrDefaultAsync();

                if (data == null) return null;

                data.BasicSalary = Utility.MaskSalary(RoleID, data.BasicSalary ?? 0);
               
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
