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
    public class EventPartnershipService : IServiceEventPartnershipAsync<EventPartnershipsDto>
    {
        private readonly EFContext _context;

        public EventPartnershipService(EFContext context)
        {
            _context = context;
        }

        private async Task ValidateSave(EventPartnershipsDto data)
        {
            if(data == null)
                throw new Exception("Data cannot be empty");

            if(string.IsNullOrEmpty(data.SchoolName))
                throw new Exception("School name cannot be empty");

            if(string.IsNullOrEmpty(data.PicName))
                throw new Exception("PIC name cannot be empty");

            if(string.IsNullOrEmpty(data.PicPhoneNumber))
                throw new Exception("PIC phone number cannot be empty");

            if(string.IsNullOrEmpty(data.PicEmail))
                throw new Exception("PIC email cannot be empty");
        }

        public async Task<ListResponse<EventPartnershipsDto>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.EventPartnerships
                            where a.IsDeleted == false
                            select new EventPartnershipsDto
                            {
                                ID = a.ID,
                                SchoolName = a.SchoolName,
                                PicName = a.PicName,
                                PicPhoneNumber = a.PicPhoneNumber,
                                PicEmail = a.PicEmail,
                                Address = a.Address
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.SchoolName.Contains(search)
                        || (x.PicName.Contains(search) || x.PicPhoneNumber.Contains(search)
                        || x.PicEmail.Contains(search))
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
                                "schoolname" => query.Where(x => x.SchoolName.Contains(value)),
                                "picname" => query.Where(x => x.PicName.Contains(value)),
                                "picphonenumber" => query.Where(x => x.PicPhoneNumber.Contains(value)),
                                "picemail" => query.Where(x => x.PicEmail.Contains(value)),
                                "address" => query.Where(x => x.Address.Contains(value)),
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
                            "schoolname" => query.OrderByDescending(x => x.SchoolName),
                            "picname" => query.OrderByDescending(x => x.PicName),
                            "picphonenumber" => query.OrderByDescending(x => x.PicPhoneNumber),
                            "picemail" => query.OrderByDescending(x => x.PicEmail),
                            "address" => query.OrderByDescending(x => x.Address),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "schoolname" => query.OrderBy(x => x.SchoolName),
                            "picname" => query.OrderBy(x => x.PicName),
                            "picphonenumber" => query.OrderBy(x => x.PicPhoneNumber),
                            "picemail" => query.OrderBy(x => x.PicEmail),
                            "address" => query.OrderBy(x => x.Address),
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

                return new ListResponse<EventPartnershipsDto>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<EventPartnershipsDto> CreateAsync(EventPartnershipsDto data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await ValidateSave(data);

                var insertedEvent = _context.Set<EventPartnershipsDto>()
                    .FromSqlRaw(@"
                        DECLARE @ID INT;
                        
                        INSERT INTO EventPartnerships (SchoolName, PicName, PicPhoneNumber, PicEmail, Address, DateIn)
                        VALUES ({0}, {1}, {2}, {3}, {4}, GETDATE());
                        
                        SET @ID = SCOPE_IDENTITY();
                        
                        SELECT *
                        FROM EventPartnerships
                        WHERE ID = @ID;
                    ", data.SchoolName, data.PicName, data.PicPhoneNumber, data.PicEmail, data.Address)
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
    }
}
