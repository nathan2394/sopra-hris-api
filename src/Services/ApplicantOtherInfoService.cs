using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Helpers;

namespace sopra_hris_api.src.Services.API
{
    public class ApplicantOtherInfoService : IServiceAsync<ApplicantOtherInfo>
    {
        private readonly EFContext _context;

        public ApplicantOtherInfoService(EFContext context)
        {
            _context = context;
        }

        public async Task<ApplicantOtherInfo> CreateAsync(ApplicantOtherInfo data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.ApplicantOtherInfo.AddAsync(data);
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
                var obj = await _context.ApplicantOtherInfo.FirstOrDefaultAsync(x => x.OtherInfoID == id && x.IsDeleted == false);
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

        public async Task<ApplicantOtherInfo> EditAsync(ApplicantOtherInfo data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.ApplicantOtherInfo.FirstOrDefaultAsync(x => x.OtherInfoID == data.OtherInfoID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.ApplicantID = data.ApplicantID;
                obj.ExpectedSalary = data.ExpectedSalary;
                obj.AvailabilityToStart = data.AvailabilityToStart;
                obj.AppliedBeforeAtSopra = data.AppliedBeforeAtSopra;
                obj.AppliedBeforeExplanation = data.AppliedBeforeExplanation;
                obj.HasRelativeAtSopra = data.HasRelativeAtSopra;
                obj.RelativeAtSopraExplanation = data.RelativeAtSopraExplanation;
                obj.AgreesToContactReferences = data.AgreesToContactReferences;
                obj.ReadyForShiftWork = data.ReadyForShiftWork;
                obj.ReadyForOutOfTownAssignments = data.ReadyForOutOfTownAssignments;
                obj.ReadyForOutOfTownPlacement = data.ReadyForOutOfTownPlacement;
                obj.HasSeriousIllnessOrInjury = data.HasSeriousIllnessOrInjury;
                obj.SeriousIllnessOrInjuryExplanation = data.SeriousIllnessOrInjuryExplanation;
                obj.HasPoliceRecord = data.HasPoliceRecord;
                obj.PoliceRecordExplanation = data.PoliceRecordExplanation;
                obj.HasPermanentPhysicalImpairment = data.HasPermanentPhysicalImpairment;
                obj.PhysicalImpairmentExplanation = data.PhysicalImpairmentExplanation;
                obj.ConflictOfInterest = data.ConflictOfInterest;
                obj.ConflictOfInterestDetails = data.ConflictOfInterestDetails;

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


        public async Task<ListResponse<ApplicantOtherInfo>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.ApplicantOtherInfo
                            where a.IsDeleted == false
                            select a;

                // Searching
                if (!string.IsNullOrEmpty(search)
                    //query = query.Where(x => x.FullName.Contains(search) || x.JobTitle.Contains(search)
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
                                "applicant" => long.TryParse(value, out var applicantId) ? query.Where(x => x.ApplicantID == applicantId) : query,
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
                    query = query.OrderByDescending(x => x.OtherInfoID);
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

                return new ListResponse<ApplicantOtherInfo>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ApplicantOtherInfo> GetByIdAsync(long id)
        {
            try
            {
                return await _context.ApplicantOtherInfo.AsNoTracking().FirstOrDefaultAsync(x => x.OtherInfoID == id && x.IsDeleted == false);
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
