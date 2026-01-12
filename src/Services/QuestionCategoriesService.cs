using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Helpers;
using System.Diagnostics;

namespace sopra_hris_api.src.Services.API
{
    public class QuestionCategorieService : IServiceAsync<QuestionCategories>
    {
        private readonly EFContext _context;

        public QuestionCategorieService(EFContext context)
        {
            _context = context;
        }

        public async Task<QuestionCategories> CreateAsync(QuestionCategories data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.QuestionCategories.AddAsync(data);
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
                var obj = await _context.QuestionCategories.FirstOrDefaultAsync(x => x.CategoryID == id && x.IsDeleted == false);
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

        public async Task<QuestionCategories> EditAsync(QuestionCategories data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.QuestionCategories.FirstOrDefaultAsync(x => x.CategoryID == data.CategoryID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.CategoryName = data.CategoryName;
                obj.Description = data.Description;
                obj.Duration = data.Duration;
                obj.TotalQuestions = data.TotalQuestions;
                obj.Weight = data.Weight;
                obj.TestType = data.TestType;

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


        public async Task<ListResponse<QuestionCategories>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                IEnumerable<QuestionCategories> query;
                long candidateid = 0;
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
                            if (fieldName.Contains("candidateid"))
                                candidateid = Convert.ToInt64(value);
                        }
                    }
                }

                if (candidateid > 0)
                {
                    var sql = @"
DECLARE @JobID BIGINT,
        @LevelID BIGINT,
        @TemplateID BIGINT;

SELECT @JobID = JobID
FROM Candidates
WHERE CandidateID = @CandidateID
  AND IsDeleted = 0;

SELECT @LevelID = LevelID
FROM Jobs
WHERE JobID = @JobID;

SELECT @TemplateID = TemplateID
FROM JobTestTemplateOverrides
WHERE JobID = @JobID;

IF @TemplateID IS NULL
BEGIN
    SELECT @TemplateID = TemplateID
    FROM LevelTestTemplates
    WHERE LevelID = @LevelID
      AND IsDefault = 1;
END

SELECT qc.*
FROM TemplateCategories tc
INNER JOIN QuestionCategories qc
    ON qc.CategoryID = tc.CategoryID
WHERE tc.TemplateID = @TemplateID
  AND tc.IsDeleted = 0
  AND qc.IsDeleted = 0
";

                    query = await _context.QuestionCategories
                        .FromSqlRaw(sql, new SqlParameter("@CandidateID", candidateid)).ToListAsync();

                }
                else
                {
                    query = await _context.QuestionCategories.Where(x => x.IsDeleted == false).ToListAsync();
                }


                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Description.Contains(search) || x.CategoryName.Contains(search)
                        );

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
                    query = query.OrderByDescending(x => x.CategoryID);
                }

                // Get Total Before Limit and Page
                total = query.Count();

                // Set Limit and Page
                if (limit != 0)
                    query = query.Skip(page * limit).Take(limit);

                // Get Data
                var data =  query.ToList();
                if (data.Count <= 0 && page > 0)
                {
                    page = 0;
                    return await GetAllAsync(limit, page, total, search, sort, filter, date);
                }

                return new ListResponse<QuestionCategories>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<QuestionCategories> GetByIdAsync(long id)
        {
            try
            {
                return await _context.QuestionCategories.AsNoTracking().FirstOrDefaultAsync(x => x.CategoryID == id && x.IsDeleted == false);
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
