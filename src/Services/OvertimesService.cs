﻿using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class OvertimeService : IServiceAsync<Overtimes>
    {
        private readonly EFContext _context;

        public OvertimeService(EFContext context)
        {
            _context = context;
        }

        public async Task<Overtimes> CreateAsync(Overtimes data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.IsApproved1 = false;
                data.IsApproved2 = false;
                await _context.Overtimes.AddAsync(data);
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
                var obj = await _context.Overtimes.FirstOrDefaultAsync(x => x.OvertimeID == id && x.IsDeleted == false);
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

        public async Task<Overtimes> EditAsync(Overtimes data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Overtimes.FirstOrDefaultAsync(x => x.OvertimeID == data.OvertimeID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.EmployeeID = data.EmployeeID;
                obj.TransDate = data.TransDate;
                obj.StartDate = data.StartDate;
                obj.EndDate = data.EndDate;
                obj.ReasonID = data.ReasonID;
                obj.Description = data.Description;
                obj.IsApproved1 = data.IsApproved1;
                obj.IsApproved2 = data.IsApproved2;

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


        public async Task<ListResponse<Overtimes>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = (from o in _context.Overtimes.AsNoTracking()
                             join e in _context.Employees on o.EmployeeID equals e.EmployeeID
                             join r in _context.Reasons on o.ReasonID equals r.ReasonID into reasonGroup
                             from r in reasonGroup.DefaultIfEmpty()
                             join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                             from d in deptGroup.DefaultIfEmpty()
                             join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                             from g in groupGroup.DefaultIfEmpty()
                             where o.IsDeleted == false
                             select new Overtimes
                             {
                                 OvertimeID = o.OvertimeID,
                                 EmployeeID = o.EmployeeID,
                                 TransDate = o.TransDate,
                                 StartDate = o.StartDate,
                                 EndDate = o.EndDate,
                                 ReasonID = o.ReasonID,
                                 IsApproved1 = o.IsApproved1,
                                 IsApproved2 = o.IsApproved2,
                                 Description = o.Description,
                                 NIK = e.Nik,
                                 EmployeeName = e.EmployeeName,
                                 DepartmentID = e.DepartmentID,
                                 DepartmentName = d != null ? d.Name : null,
                                 GroupID = e.GroupID,
                                 GroupName = g != null ? g.Name : null,
                                 GroupType = g != null ? g.Type : null,
                                 ReasonCode = r != null ? r.Code : null,
                                 ReasonName = r != null ? r.Name : null
                             });

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Description.Contains(search)
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
                                "name" => query.Where(x => x.Description.Contains(value)),
                                "reason" => query.Where(x => x.ReasonCode.Contains(value)),
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
                            "reason" => query.OrderByDescending(x => x.ReasonName),
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            "startdate" => query.OrderByDescending(x => x.StartDate),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "department" => query.OrderByDescending(x => x.DepartmentName),
                            "group" => query.OrderByDescending(x => x.GroupType),
                            "reason" => query.OrderByDescending(x => x.ReasonName),
                            "name" => query.OrderByDescending(x => x.EmployeeName),
                            "startdate" => query.OrderByDescending(x => x.StartDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.OvertimeID);
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

                return new ListResponse<Overtimes>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Overtimes> GetByIdAsync(long id)
        {
            try
            {
                return await (from o in _context.Overtimes.AsNoTracking()
                              join e in _context.Employees on o.EmployeeID equals e.EmployeeID
                              join r in _context.Reasons on o.ReasonID equals r.ReasonID into reasonGroup
                              from r in reasonGroup.DefaultIfEmpty()
                              join d in _context.Departments on e.DepartmentID equals d.DepartmentID into deptGroup
                              from d in deptGroup.DefaultIfEmpty()
                              join g in _context.Groups on e.GroupID equals g.GroupID into groupGroup
                              from g in groupGroup.DefaultIfEmpty()
                              where o.OvertimeID == id && o.IsDeleted == false
                              select new Overtimes
                              {
                                  OvertimeID = o.OvertimeID,
                                  EmployeeID = o.EmployeeID,
                                  TransDate = o.TransDate,
                                  StartDate = o.StartDate,
                                  EndDate = o.EndDate,
                                  ReasonID = o.ReasonID,
                                  IsApproved1 = o.IsApproved1,
                                  IsApproved2 = o.IsApproved2,
                                  Description = o.Description,
                                  NIK = e.Nik,
                                  EmployeeName = e.EmployeeName,
                                  DepartmentID = e.DepartmentID,
                                  DepartmentName = d != null ? d.Name : null,
                                  GroupID = e.GroupID,
                                  GroupName = g != null ? g.Name : null,
                                  GroupType = g != null ? g.Type : null,
                                  ReasonCode = r != null ? r.Code : null,
                                  ReasonName = r != null ? r.Name : null
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
