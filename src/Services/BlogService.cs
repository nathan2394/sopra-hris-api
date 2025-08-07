using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using System.Collections.Generic;
using sopra_hris_api.Responses;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Services;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.Services
{
    public class BlogService: IServiceAsync<Blogs>
    {
        private readonly EFContext _context;
        public BlogService(EFContext context)
        {
            _context = context;
        }

        public async Task<ListResponse<Blogs>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Blogs where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.BlogTitle_id.Contains(search) || x.BlogTitle_en.Contains(search)
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
                                "title" => query.Where(x => x.BlogTitle_id.Contains(value) || x.BlogTitle_en.Contains(value)),
                                "tags" => query.Where(x => x.BlogTags.Contains(value)),
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
                            "title_en" => query.OrderByDescending(x => x.BlogTitle_en),
                            "title_id" => query.OrderByDescending(x => x.BlogTitle_id),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "title_id" => query.OrderBy(x => x.BlogTitle_id),
                            "title_en" => query.OrderBy(x => x.BlogTitle_en),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.BlogID);
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
                return new ListResponse<Blogs>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Blogs> CreateAsync(Blogs data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Blogs.AddAsync(data);
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
                var obj = await _context.Blogs.FirstOrDefaultAsync(x => x.BlogID == id && x.IsDeleted == false);
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

        public async Task<Blogs> EditAsync(Blogs data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Blogs.FirstOrDefaultAsync(x => x.BlogID == data.BlogID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.BlogTitle_id = data.BlogTitle_id;
                obj.BlogTitle_en = data.BlogTitle_en;
                obj.BlogImage = data.BlogImage;
                obj.BlogContent_id = data.BlogContent_id;
                obj.BlogContent_en = data.BlogContent_en;
                obj.BlogThumbnail = data.BlogThumbnail;
                obj.BlogVideo = data.BlogVideo;
                obj.BlogTags = data.BlogTags;

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


        public async Task<Blogs> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Blogs.AsNoTracking().FirstOrDefaultAsync(x => x.BlogID == id && x.IsDeleted == false);
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