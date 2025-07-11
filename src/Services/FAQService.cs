﻿using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class FAQService : IServiceAsync<FAQ>
    {
        private readonly EFContext _context;

        public FAQService(EFContext context)
        {
            _context = context;
        }

        public async Task<FAQ> CreateAsync(FAQ data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.FAQ.AddAsync(data);
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
                var obj = await _context.FAQ.FirstOrDefaultAsync(x => x.FAQID == id && x.IsDeleted == false);
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

        public async Task<FAQ> EditAsync(FAQ data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.FAQ.FirstOrDefaultAsync(x => x.FAQID == data.FAQID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.Category= data.Category;
                obj.SubCategory = data.SubCategory;
                obj.Question = data.Question;
                obj.Answer = data.Answer;

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


        public async Task<ListResponse<FAQ>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.FAQ where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Category.Contains(search) || x.SubCategory.Contains(search) || x.Question.Contains(search) || x.Answer.Contains(search)
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
                                "category" => query.Where(x => x.Category.Contains(value)),
                                "subcategory" => query.Where(x => x.SubCategory.Contains(value)),
                                "question" => query.Where(x => x.Question.Contains(value)),
                                "answer" => query.Where(x => x.Answer.Contains(value)),
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
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.FAQID);
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

                return new ListResponse<FAQ>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<FAQ> GetByIdAsync(long id)
        {
            try
            {
                return await _context.FAQ.AsNoTracking().FirstOrDefaultAsync(x => x.FAQID == id && x.IsDeleted == false);
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
