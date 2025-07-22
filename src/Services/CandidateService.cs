using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class CandidateService : IServiceAsync<Candidates>
    {
        private readonly EFContext _context;

        public CandidateService(EFContext context)
        {
            _context = context;
        }

        public async Task<Candidates> CreateAsync(Candidates data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Candidates.AddAsync(data);
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
                var obj = await _context.Candidates.FirstOrDefaultAsync(x => x.CandidateID == id && x.IsDeleted == false);
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

        public async Task<Candidates> EditAsync(Candidates data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Candidates.FirstOrDefaultAsync(x => x.CandidateID == data.CandidateID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.CandidateName = data.CandidateName;
                obj.Email = data.Email;
                obj.PhoneNumber = data.PhoneNumber;
                obj.ResumeURL = data.ResumeURL;
                obj.Remarks = data.Remarks;
                obj.JobID = data.JobID;

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


        public async Task<ListResponse<Candidates>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from c in _context.Candidates
                            join j in _context.Jobs on c.JobID equals j.JobID
                            where c.IsDeleted == false
                            select new Candidates
                            {
                                CandidateID = c.CandidateID,
                                CandidateName = c.CandidateName,
                                JobID = c.JobID,
                                JobTitle = j.JobTitle,
                                Email = c.Email,
                                PhoneNumber = c.PhoneNumber,
                                ResumeURL = c.ResumeURL,
                                Remarks = c.Remarks
                            };

                // Searching 
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.JobTitle.Contains(search)
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
                                "name" => query.Where(x => x.CandidateName.Contains(value)),
                                "jobtitle" => query.Where(x => x.JobTitle.Contains(value)),
                                "jobid" => query.Where(x => x.JobID.Equals(value)),
                                "phonenumber" => query.Where(x => x.PhoneNumber.Contains(value)),
                                "email" => query.Where(x => x.Email.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.CandidateName),
                            "jobtitle" => query.OrderByDescending(x => x.JobTitle),
                            "jobid" => query.OrderByDescending(x => x.JobID),
                            "phonenumber" => query.OrderByDescending(x => x.PhoneNumber),
                            "email" => query.OrderByDescending(x => x.Email),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.CandidateName),
                            "jobtitle" => query.OrderBy(x => x.JobTitle),
                            "jobid" => query.OrderBy(x => x.JobID),
                            "phonenumber" => query.OrderBy(x => x.PhoneNumber),
                            "email" => query.OrderBy(x => x.Email),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.CandidateID);
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

                return new ListResponse<Candidates>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Candidates> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Candidates.AsNoTracking().FirstOrDefaultAsync(x => x.CandidateID == id && x.IsDeleted == false);
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
