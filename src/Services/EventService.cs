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
    public class EventService : IServiceEventAsync<Events>
    {
        private readonly EFContext _context;
        private static readonly string[] AllowedImageExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

        public EventService(EFContext context)
        {
            _context = context;
        }

        private async Task ValidateSave(EventsDto data)
        {
            if(data == null)
                throw new Exception("Event data cannot be empty");

            if(data.StartDate > data.EndDate)
                throw new Exception("Event start date cannot be greater than end date");
            
            if(data.StartDate < DateTime.Now)
                throw new Exception("Event start date cannot be in the past");
        }

        public async Task<ListResponse<EventListDto>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Events
                            where a.IsDeleted == false
                            select new EventListDto
                            {
                                ID = a.ID,
                                Name = a.Name,
                                Image = a.Image,
                                Location = a.Location,
                                Program = a.Program,
                                StartDate = a.StartDate,
                                EndDate = a.EndDate,
                                Type = a.Type,
                                LocationLink = a.LocationLink
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Contains(search)
                        || (x.Image.Contains(search) || x.Location.Contains(search)
                        || x.Program.Contains(search))
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
                                "name" => query.Where(x => x.Name.Contains(value)),
                                "image" => query.Where(x => x.Image.Contains(value)),
                                "location" => query.Where(x => x.Location.Contains(value)),
                                "program" => query.Where(x => x.Program.Contains(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                _ => query
                            };
                        }
                    }
                }

                // Date Filtering
                if (!string.IsNullOrEmpty(date))
                {
                    DateTime.TryParse(date, out var queryDate);
                    query = query.Where(x => (x.StartDate == null || x.EndDate == null || (x.StartDate.Value.Date <= queryDate && x.EndDate.Value.Date >= queryDate)));
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
                            "name" => query.OrderByDescending(x => x.Name),
                            "startdate" => query.OrderByDescending(x => x.StartDate),
                            "enddate" => query.OrderByDescending(x => x.EndDate),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderByDescending(x => x.Name),
                            "startdate" => query.OrderBy(x => x.StartDate),
                            "enddate" => query.OrderBy(x => x.EndDate),
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

                return new ListResponse<EventListDto>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<EventsDto> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.Set<EventsDto>()
                    .FromSqlRaw(@"
                    SELECT *
                    FROM Events
                    WHERE ID = {0}
                        AND IsDeleted = 0
                ", id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

                return data ?? new EventsDto();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<EventsDto> CreateAsync(EventsDto data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await ValidateSave(data);

                var insertedEvent = _context.Set<EventsDto>()
                    .FromSqlRaw(@"
                        DECLARE @ID INT;
                        
                        INSERT INTO Events (Name, Image, Location, Program, Type, StartDate, EndDate, LocationLink, UserIn, DateIn)
                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, GETDATE());
                        
                        SET @ID = SCOPE_IDENTITY();
                        
                        SELECT *
                        FROM Events
                        WHERE ID = @ID;
                    ", data.Name, data.Image, data.Location, data.Program, data.Type, data.StartDate, data.EndDate, data.LocationLink, userID)
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

        public async Task<EventsDto> EditAsync(EventsDto data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await ValidateSave(data);

                var obj = await _context.Set<EventsDto>()
                    .FromSqlRaw(@"
                    SELECT *
                    FROM Events
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
                        Name = {1},
                        Image = {2},
                        Location = {3},
                        Program = {4},
                        Type = {5},
                        StartDate = {6},
                        EndDate = {7},
                        LocationLink = {8},
                        UserUp = {9},
                        DateUp = GETDATE()
                    WHERE ID = {0}

                    SET @ID = {0}

                    SELECT
                        ID,
                        Name,
                        Image,
                        Location,
                        Program,
                        Type,
                        StartDate,
                        EndDate,
                        LocationLink
                    FROM Events
                    WHERE ID = @ID;
                ", data.ID, data.Name, data.Image, data.Location, data.Program, data.Type, data.StartDate, data.EndDate, data.LocationLink, userID);

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
                var obj = await _context.Set<EventsDto>()
                    .FromSqlRaw(@"
                    SELECT *
                    FROM Events
                    WHERE ID = {0}
                        AND IsDeleted = 0
                ", id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

                if (obj == null) return false;

                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE Events
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

        public async Task<string> UploadAsync(IFormFile file, long userID)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new Exception("No file uploaded.");

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(fileExtension))
                    throw new Exception("Only image files are allowed.");

                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles", "Events");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var safeFileName = Path.GetFileName(file.FileName);
                var generatedFileName = $"{Guid.NewGuid():N}_{safeFileName}";
                var filePath = Path.Combine(folderPath, generatedFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                return Path.Combine("UploadedFiles", "Events", generatedFileName).Replace("\\", "/");
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
