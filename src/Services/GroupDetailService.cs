﻿using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class GroupDetailService : IServiceAsync<GroupDetails>
    {
        private readonly EFContext _context;

        public GroupDetailService(EFContext context)
        {
            _context = context;
        }

        public async Task<GroupDetails> CreateAsync(GroupDetails data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.GroupDetails.AddAsync(data);
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
                var obj = await _context.GroupDetails.FirstOrDefaultAsync(x => x.GroupDetailID == id && x.IsDeleted == false);
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

        public async Task<GroupDetails> EditAsync(GroupDetails data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.GroupDetails.FirstOrDefaultAsync(x => x.GroupDetailID == data.GroupDetailID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.AllowanceDeductionID = data.AllowanceDeductionID;
                obj.Amount = data.Amount;

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


        public async Task<ListResponse<GroupDetails>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.GroupDetails
                            where a.IsDeleted == false
                            join ad in _context.AllowanceDeduction
                            on a.AllowanceDeductionID equals ad.AllowanceDeductionID
                            select new GroupDetails
                            {
                                GroupDetailID = a.GroupDetailID,
                                GroupID = a.GroupID,
                                AllowanceDeductionID = ad.AllowanceDeductionID,
                                Amount = a.Amount,
                                AllowanceDeductionType = ad.Type,
                                AllowanceDeductionName = ad.Name
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.AllowanceDeductionName.Contains(search)
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
                                "group" => query.Where(x => x.GroupID.ToString().Equals(value)),
                                "allowancededuction" => query.Where(x => x.AllowanceDeductionID.ToString().Contains(value)),
                                "name" => query.Where(x => x.AllowanceDeductionName.ToString().Contains(value)),
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
                            "allowancededuction" => query.OrderByDescending(x => x.AllowanceDeductionID),
                            "name" => query.OrderByDescending(x => x.AllowanceDeductionName),
                            "id" => query.OrderByDescending(x => x.GroupDetailID),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "allowancededuction" => query.OrderBy(x => x.AllowanceDeductionID),
                            "name" => query.OrderBy(x => x.AllowanceDeductionName),
                            "id" => query.OrderBy(x => x.GroupDetailID),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.GroupDetailID);
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

                return new ListResponse<GroupDetails>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<GroupDetails> GetByIdAsync(long id)
        {
            try
            {
                return await (from a in _context.GroupDetails
                              join ad in _context.AllowanceDeduction
                              on a.AllowanceDeductionID equals ad.AllowanceDeductionID
                              where a.GroupDetailID == id && a.IsDeleted == false
                              select new GroupDetails
                              {
                                  GroupDetailID = a.GroupDetailID,
                                  GroupID = a.GroupID,
                                  AllowanceDeductionID = a.AllowanceDeductionID,
                                  Amount = a.Amount,
                                  AllowanceDeductionName = ad.Name,
                                  AllowanceDeductionType = ad.Type
                              })
                            .AsNoTracking()
                            .FirstOrDefaultAsync();
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
