using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Entities;
using sopra_hris_api.src.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace sopra_hris_api.src.Services.API
{
    public class NotificationService : IServiceNotificationAsync<Notifications>
    {
        private readonly EFContext _context;
        private static readonly string[] AllowedImageExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

        public NotificationService(EFContext context)
        {
            _context = context;
        }

        public async Task<ListResponse<Notifications>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Notifications
                            where a.IsDeleted == false
                            select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Title.Contains(search)
                        || (x.Description.Contains(search))
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
                                "title" => query.Where(x => x.Title.Contains(value)),
                                "description" => query.Where(x => x.Description.Contains(value)),
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
                            "title" => query.OrderByDescending(x => x.Title),
                            "description" => query.OrderByDescending(x => x.Description),
                            "date" => query.OrderByDescending(x => x.DateIn),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "title" => query.OrderByDescending(x => x.Title),
                            "description" => query.OrderByDescending(x => x.Description),
                            "date" => query.OrderBy(x => x.DateIn),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.ID);
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

                return new ListResponse<Notifications>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Notifications> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.Set<Notifications>()
                    .FromSqlRaw(@"
                    SELECT *
                    FROM Notifications
                    WHERE ID = {0}
                        AND IsDeleted = 0
                ", id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

                return data ?? new Notifications();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Notifications> CreateAsync(Notifications data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var insertedEvent = _context.Set<Notifications>()
                    .FromSqlRaw(@"
                        DECLARE @ID INT;
                        
                        INSERT INTO Notifications (Title, Description, CompanyID, UserIn, DateIn)
                        VALUES ({0}, {1}, {2}, {3}, GETDATE());
                        
                        SET @ID = SCOPE_IDENTITY();
                        
                        SELECT *
                        FROM Notifications
                        WHERE ID = @ID;
                    ", data.Title, data.Description, data.CompanyID, userID)
                    .AsEnumerable()
                    .FirstOrDefault();

                await dbTrans.CommitAsync();

                return insertedEvent;
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

        public async Task<Notifications> EditAsync(Notifications data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Set<Notifications>()
                    .FromSqlRaw(@"
                    SELECT *
                    FROM Notifications
                    WHERE ID = {0}
                        AND IsDeleted = 0
                ", data.ID)
                .AsNoTracking()
                .FirstOrDefaultAsync();

                if (obj == null) return null;

                var updatedEvent = await _context.Database.ExecuteSqlRawAsync(@"
                    DECLARE @ID INT;
                
                    UPDATE Events
                    SET
                        Title = {1},
                        Description = {2},
                        CompanyID = {3},
                        UserUp = {4},
                        DateUp = GETDATE()
                    WHERE ID = {0}

                    SET @ID = {0}

                    SELECT
                        ID,
                        Title,
                        Description,
                        CompanyID,
                        UserIn,
                        DateIn
                    FROM Notifications
                    WHERE ID = @ID;
                ", data.ID, data.Title, data.Description, data.CompanyID, userID);

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

        public async Task<bool> DeleteAsync(long id, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Set<Notifications>()
                    .FromSqlRaw(@"
                    SELECT *
                    FROM Notifications
                    WHERE ID = {0}
                        AND IsDeleted = 0
                ", id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

                if (obj == null) return false;

                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE Notifications
                    SET
                        IsDeleted = 1,
                        UserUp = {1},
                        DateUp = GETDATE()
                    WHERE ID = {0};
                ", obj.ID, userID);

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
    }
}
