﻿using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class UnattendanceService : IServiceAsync<Unattendances>
    {
        private readonly EFContext _context;

        public UnattendanceService(EFContext context)
        {
            _context = context;
        }

        public async Task<Unattendances> CreateAsync(Unattendances data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.IsApproved1 = false;
                data.IsApproved2 = false;
                await _context.Unattendances.AddAsync(data);
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
                var obj = await _context.Unattendances.FirstOrDefaultAsync(x => x.UnattendanceID == id && x.IsDeleted == false);
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

        public async Task<Unattendances> EditAsync(Unattendances data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Unattendances.FirstOrDefaultAsync(x => x.UnattendanceID == data.UnattendanceID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.StartDate = data.StartDate;
                obj.EndDate = data.EndDate;
                obj.UnattendanceTypeID = data.UnattendanceTypeID;
                obj.IsApproved1 = data.IsApproved1;
                obj.IsApproved2 = data.IsApproved2;
                obj.Description = data.Description;
                obj.Duration = data.Duration;

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


        public async Task<ListResponse<Unattendances>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from u in _context.Unattendances
                            join e in _context.Employees on u.EmployeeID equals e.EmployeeID
                            join ut in _context.UnattendanceTypes on u.UnattendanceTypeID equals ut.UnattendanceTypeID
                            join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                            from d in deptGroup.DefaultIfEmpty()
                            join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                            from g in groupGroup.DefaultIfEmpty()
                            where u.IsDeleted == false
                            select new Unattendances
                            {
                                UnattendanceID = u.UnattendanceID,
                                EmployeeID = u.EmployeeID,
                                StartDate = u.StartDate,
                                EndDate = u.EndDate,
                                UnattendanceTypeID = u.UnattendanceTypeID,
                                IsApproved1 = u.IsApproved1,
                                IsApproved2 = u.IsApproved2,
                                Description = u.Description,
                                NIK = e.Nik,
                                EmployeeName = e.EmployeeName,
                                DepartmentName = d.Name,
                                DepartmentID = e.DepartmentID,
                                GroupID = e.GroupID,
                                GroupName = g.Name,
                                GroupType = g.Type,
                                UnattendanceTypeCode = ut.Code,
                                UnattendanceTypeName = ut.Name,
                                Duration = u.Duration
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Description.Contains(search) || x.UnattendanceTypeName.Contains(search) || x.EmployeeName.Contains(search) || x.DepartmentName.Contains(search) || x.GroupName.Contains(search)
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
                            if (fieldName == "group" || fieldName == "department")
                            {
                                var Ids = value.Split(',').Select(v => long.Parse(v.Trim())).ToList();
                                if (fieldName == "group")
                                    query = query.Where(x => Ids.Contains(x.GroupID ?? 0));
                                else if (fieldName == "department")
                                    query = query.Where(x => Ids.Contains(x.DepartmentID ?? 0));
                            }
                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.EmployeeName.Contains(value)),
                                "unattendancetype" => query.Where(x => x.UnattendanceTypeName.Contains(value)),
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
                            "department" => query.OrderByDescending(x => x.DepartmentName),
                            "group" => query.OrderByDescending(x => x.GroupType),
                            "unattendance" => query.OrderByDescending(x => x.UnattendanceTypeName),
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            "startdate" => query.OrderByDescending(x => x.StartDate),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "department" => query.OrderBy(x => x.DepartmentName),
                            "group" => query.OrderBy(x => x.GroupType),
                            "unattendance" => query.OrderBy(x => x.UnattendanceTypeName),
                            "name" => query.OrderBy(x => x.EmployeeName),
                            "startdate" => query.OrderBy(x => x.StartDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.UnattendanceID);
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

                return new ListResponse<Unattendances>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Unattendances> GetByIdAsync(long id)
        {
            try
            {
                return await (from u in _context.Unattendances.AsNoTracking()
                                    join e in _context.Employees on u.EmployeeID equals e.EmployeeID
                                    join ut in _context.UnattendanceTypes on u.UnattendanceTypeID equals ut.UnattendanceTypeID
                                    join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                                    from d in deptGroup.DefaultIfEmpty()
                                    join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                                    from g in groupGroup.DefaultIfEmpty()
                                    where u.UnattendanceID == id && u.IsDeleted == false
                                    select new Unattendances
                                    {
                                        UnattendanceID = u.UnattendanceID,
                                        EmployeeID = u.EmployeeID,
                                        StartDate = u.StartDate,
                                        EndDate = u.EndDate,
                                        UnattendanceTypeID = u.UnattendanceTypeID,
                                        IsApproved1 = u.IsApproved1,
                                        IsApproved2 = u.IsApproved2,
                                        Description = u.Description,
                                        NIK = e.Nik,
                                        EmployeeName = e.EmployeeName,
                                        DepartmentName = d.Name,
                                        DepartmentID = e.DepartmentID,
                                        GroupID = e.GroupID,
                                        GroupName = g.Name,
                                        GroupType = g.Type,
                                        UnattendanceTypeCode = ut.Code,
                                        UnattendanceTypeName = ut.Name,
                                        Duration = u.Duration
                                    }).AsNoTracking().FirstOrDefaultAsync();
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
