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
    public class LeadCandidateService : IServiceLeadCandidateAsync<LeadCandidatesDto>
    {
        private readonly EFContext _context;

        public LeadCandidateService(EFContext context)
        {
            _context = context;
        }

        private async Task ValidateSave(LeadCandidatesDto data)
        {
            if(data == null)
                throw new Exception("Data cannot be empty");

            if(string.IsNullOrEmpty(data.FullName))
                throw new Exception("Full name cannot be empty");

            if(string.IsNullOrEmpty(data.PhoneNumber))
                throw new Exception("Phone number cannot be empty");

            if(string.IsNullOrEmpty(data.Email))
                throw new Exception("Email cannot be empty");

            if(string.IsNullOrEmpty(data.Major))
                throw new Exception("Major cannot be empty");
            
            if(data.JobID <= 0)
                throw new Exception("Interest area cannot be empty");

            if(data.EventID <= 0)
                throw new Exception("Event cannot be empty");
        }

        public async Task<ListResponse<LeadCandidatesDto>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.LeadCandidates
                            where a.IsDeleted == false
                            select new LeadCandidatesDto
                            {
                                ID = a.ID,
                                FullName = a.FullName,
                                PhoneNumber = a.PhoneNumber,
                                SchoolName = a.SchoolName,
                                LastEducation = a.LastEducation,
                                YearGraduated = a.YearGraduated,
                                Major = a.Major,
                                JobID = a.JobID,
                                LastExperience = a.LastExperience,
                                EventID = a.EventID
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.FullName.Contains(search)
                        || (x.PhoneNumber.Contains(search) || x.SchoolName.Contains(search)
                        || x.LastEducation.Contains(search))
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
                                "fullname" => query.Where(x => x.FullName.Contains(value)),
                                "phonenumber" => query.Where(x => x.PhoneNumber.Contains(value)),
                                "schoolname" => query.Where(x => x.SchoolName.Contains(value)),
                                "lasteducation" => query.Where(x => x.LastEducation.Contains(value)),
                                "major" => query.Where(x => x.Major.Contains(value)),
                                "lastexperience" => query.Where(x => x.LastExperience.Contains(value)),
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
                            "fullname" => query.OrderByDescending(x => x.FullName),
                            "phonenumber" => query.OrderByDescending(x => x.PhoneNumber),
                            "schoolname" => query.OrderByDescending(x => x.SchoolName),
                            "lasteducation" => query.OrderByDescending(x => x.LastEducation),
                            "major" => query.OrderByDescending(x => x.Major),
                            "lastexperience" => query.OrderByDescending(x => x.LastExperience),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "fullname" => query.OrderBy(x => x.FullName),
                            "phonenumber" => query.OrderBy(x => x.PhoneNumber),
                            "schoolname" => query.OrderBy(x => x.SchoolName),
                            "lasteducation" => query.OrderBy(x => x.LastEducation),
                            "major" => query.OrderBy(x => x.Major),
                            "lastexperience" => query.OrderBy(x => x.LastExperience),
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

                return new ListResponse<LeadCandidatesDto>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<LeadCandidatesDto> CreateAsync(LeadCandidatesDto data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await ValidateSave(data);

                var insertedEvent = _context.Set<LeadCandidatesDto>()
                    .FromSqlRaw(@"
                        DECLARE @ID INT;
                        
                        INSERT INTO LeadCandidates (FullName, PhoneNumber, Email, SchoolName, LastEducation, YearGraduated, Major, JobID, LastExperience, EventID, DateIn)
                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, GETDATE());
                        
                        SET @ID = SCOPE_IDENTITY();
                        
                        SELECT
                            ID,
                            ISNULL(FullName, '') AS FullName,
                            ISNULL(PhoneNumber, '') AS PhoneNumber,
                            ISNULL(Email, '') AS Email,
                            ISNULL(SchoolName, '') AS SchoolName,
                            ISNULL(LastEducation, '') AS LastEducation,
                            ISNULL(YearGraduated, 0) AS YearGraduated,
                            ISNULL(Major, '') AS Major,
                            JobID,
                            ISNULL(LastExperience, '') AS LastExperience,
                            EventID
                        FROM LeadCandidates
                        WHERE ID = @ID;
                    ", data.FullName, data.PhoneNumber, data.Email, data.SchoolName ?? string.Empty, data.LastEducation ?? string.Empty, data.YearGraduated, data.Major, data.JobID, data.LastExperience ?? string.Empty, data.EventID)
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
