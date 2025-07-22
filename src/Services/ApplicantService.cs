using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class ApplicantService : IServiceAsync<Applicants>
    {
        private readonly EFContext _context;

        public ApplicantService(EFContext context)
        {
            _context = context;
        }

        public async Task<Applicants> CreateAsync(Applicants data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Applicants.AddAsync(data);
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
                var obj = await _context.Applicants.FirstOrDefaultAsync(x => x.ApplicantID == id && x.IsDeleted == false);
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

        public async Task<Applicants> EditAsync(Applicants data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Applicants.FirstOrDefaultAsync(x => x.ApplicantID == data.ApplicantID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.JobID = data.JobID;
                obj.CandidateID = data.CandidateID;
                obj.Status = data.Status;

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


        public async Task<ListResponse<Applicants>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Applicants
                            join c in _context.Candidates on a.CandidateID equals c.CandidateID
                            join j in _context.Jobs on a.JobID equals j.JobID
                            where a.IsDeleted == false
                            select new Applicants
                            {
                                ApplicantID = a.ApplicantID,
                                CandidateID = a.CandidateID,
                                JobID = a.JobID,
                                JobTitle = j.JobTitle,
                                Status = a.Status,
                                FullName = a.FullName,
                                Gender = a.Gender,
                                Address = a.Address,
                                BloodType = a.BloodType,
                                DateOfBirth = a.DateOfBirth,
                                HeightCM = a.HeightCM,
                                Email = a.Email,
                                HomePhoneNumber = a.HomePhoneNumber,
                                MobilePhoneNumber = a.MobilePhoneNumber,
                                Religion = a.Religion,
                                PlaceOfBirth = a.PlaceOfBirth,
                                MaritalStatus = a.MaritalStatus,
                                NoKTP = a.NoKTP,
                                NoSIM = a.NoSIM,
                                WeightKG = a.WeightKG,
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.FullName.Contains(search) || x.JobTitle.Contains(search)
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
                                "name" => query.Where(x => x.FullName.Contains(value)),
                                "jobtitle" => query.Where(x => x.JobTitle.Contains(value)),
                                "jobid" => query.Where(x => x.JobID.Equals(value)),
                                "status" => query.Where(x => x.Status.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.FullName),
                            "jobtitle" => query.OrderByDescending(x => x.JobTitle),
                            "status" => query.OrderByDescending(x => x.Status),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.FullName),
                            "jobtitle" => query.OrderBy(x => x.JobTitle),
                            "status" => query.OrderBy(x => x.Status),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.ApplicantID);
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

                return new ListResponse<Applicants>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Applicants> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Applicants.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicantID == id && x.IsDeleted == false);
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
