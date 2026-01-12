using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using Microsoft.Data.SqlClient;

namespace sopra_hris_api.src.Services.API
{
    public class TestResponseService : IServiceAsync<TestResponses>
    {
        private readonly EFContext _context;

        public TestResponseService(EFContext context)
        {
            _context = context;
        }

        public async Task<TestResponses> CreateAsync(TestResponses data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if response for this session and question already exists
                var existingResponse = await _context.TestResponses
                    .FirstOrDefaultAsync(x => x.SessionID == data.SessionID 
                        && x.QuestionID == data.QuestionID 
                        && x.IsDeleted == false);

                if (existingResponse != null)
                {
                    // Update existing response
                    existingResponse.SelectedAnswerID = data.SelectedAnswerID;
                    existingResponse.UserUp = data.UserUp;
                    existingResponse.DateUp = DateTime.Now;
                    
                    _context.TestResponses.Update(existingResponse);
                }
                else
                {
                    // Insert new response
                    await _context.TestResponses.AddAsync(data);
                }

                await _context.SaveChangesAsync();

                // Auto-calculate and update TechnicalTestScore in Candidates table using weighted formula
                var session = await _context.TestSessions
                    .FirstOrDefaultAsync(x => x.SessionID == data.SessionID && x.IsDeleted == false);

                if (session != null)
                {
                    var candidate = await _context.Candidates
                        .FirstOrDefaultAsync(x => x.CandidateID == session.CandidateID && x.IsDeleted == false);

                    if (candidate != null)
                    {
                        await CalculateWeightedScoreAsync(data.SessionID);
                    }
                }

                await dbTrans.CommitAsync();

                return existingResponse ?? data;
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
                var obj = await _context.TestResponses.FirstOrDefaultAsync(x => x.ResponseID == id && x.IsDeleted == false);
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

        public async Task<TestResponses> EditAsync(TestResponses data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.TestResponses.FirstOrDefaultAsync(x => x.ResponseID == data.ResponseID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.SessionID = data.SessionID;
                obj.QuestionID = data.QuestionID;
                obj.SelectedAnswerID = data.SelectedAnswerID;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Auto-calculate and update TechnicalTestScore in Candidates table using weighted formula
                var session = await _context.TestSessions
                    .FirstOrDefaultAsync(x => x.SessionID == obj.SessionID && x.IsDeleted == false);

                if (session != null)
                {
                    var candidate = await _context.Candidates
                        .FirstOrDefaultAsync(x => x.CandidateID == session.CandidateID && x.IsDeleted == false);

                    if (candidate != null)
                    {
                        await CalculateWeightedScoreAsync(obj.SessionID);
                    }
                }

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


        public async Task<ListResponse<TestResponses>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.TestResponses where a.IsDeleted == false select a;

                // Searching
                //if (!string.IsNullOrEmpty(search))
                //    query = query.Where(x => x.Name.Contains(search)
                //        );

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
                    query = query.OrderByDescending(x => x.ResponseID);
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

                return new ListResponse<TestResponses>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<TestResponses> GetByIdAsync(long id)
        {
            try
            {
                return await _context.TestResponses.AsNoTracking().FirstOrDefaultAsync(x => x.ResponseID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        private async Task CalculateWeightedScoreAsync(long sessionId)
        {
            try
            {
                var query = @";WITH AnswerScoring AS (
    SELECT
        ao.AnswerID,
        ao.QuestionID,
        ao.IsCorrect,
        ao.ScoreValue,
        EffectiveScore =
            CASE 
                WHEN (ao.ScoreValue = 0 AND (ao.IsCorrect = 0 OR ao.IsCorrect = 1))
                    THEN CASE WHEN ao.IsCorrect = 1 THEN 1.0 ELSE 0.0 END
                WHEN (ao.IsCorrect = 0 AND ao.ScoreValue > 0)
                    THEN CAST(ao.ScoreValue AS decimal(10,4))
                ELSE CAST(ao.ScoreValue AS decimal(10,4))
            END
    FROM AnswerOptions ao
),
QuestionMax AS (
    SELECT
        QuestionID,
        MAX(EffectiveScore) AS MaxScoreValue
    FROM AnswerScoring
    GROUP BY QuestionID
),
CategoryScore AS (
    SELECT
        ts.SessionID,
        ts.CandidateID,
        qc.CategoryID,
        qc.CategoryName,
        qc.TestType,
        qc.Weight,
        qc.TotalQuestions AS ConfigTotalQuestions,

        SUM(
            CASE WHEN tr.SelectedAnswerID > 0 
                 THEN asg.EffectiveScore 
                 ELSE 0 
            END
        ) AS TotalScoreCategoryRaw,

        MAX(qm.MaxScoreValue) AS MaxScorePerQuestion,

        qc.TotalQuestions * MAX(qm.MaxScoreValue) AS CategoryMaxScore,

        COUNT(DISTINCT CASE WHEN tr.SelectedAnswerID > 0 THEN tr.QuestionID END) AS AnsweredQuestionCount,

        SUM(
            CASE 
                WHEN tr.SelectedAnswerID > 0 AND asg.IsCorrect = 1 
                    THEN 1 
                ELSE 0 
            END
        ) AS CorrectCount,

        CAST(
            CASE 
                WHEN qc.TotalQuestions = 0 
                     OR MAX(qm.MaxScoreValue) = 0
                    THEN 0
                WHEN MAX(qm.MaxScoreValue)=1 THEN
                    qc.Weight * SUM(CASE WHEN tr.SelectedAnswerID > 0 AND IsCorrect=1 THEN 1 ELSE 0 END)
                         / (qc.TotalQuestions * MAX(qm.MaxScoreValue))
                ELSE qc.Weight * SUM(CASE WHEN tr.SelectedAnswerID > 0 THEN asg.EffectiveScore ELSE 0 END)
                         / (qc.TotalQuestions * MAX(qm.MaxScoreValue))
            END
        AS decimal(5,2)) AS CategoryPct
    FROM TestResponses        tr
    INNER JOIN TestSessions         ts  ON tr.SessionID        = ts.SessionID
    INNER JOIN Questions            q   ON tr.QuestionID       = q.QuestionID
    INNER JOIN QuestionCategories   qc  ON q.CategoryID        = qc.CategoryID
    INNER JOIN AnswerScoring        asg ON tr.SelectedAnswerID = asg.AnswerID
    INNER JOIN QuestionMax          qm  ON qm.QuestionID       = tr.QuestionID
    WHERE ts.SessionID=@SessionID
    GROUP BY
        ts.SessionID,
        ts.CandidateID,
        qc.CategoryID,
        qc.CategoryName,
        qc.TestType,
        qc.Weight,
        qc.TotalQuestions
),
TypeScore AS (
    SELECT
        SessionID,
        CandidateID,
        -- TechnicalScore
        CAST(SUM(CASE WHEN TestType = 'Technical' THEN CategoryPct  END)
        AS decimal(5,2)) AS TechnicalScore,

        -- CognitiveScore
        CAST(SUM(CASE WHEN TestType = 'Cognitive' THEN CategoryPct END)
        AS decimal(5,2)) AS CognitiveScore,

        -- BehaviourScore
        CAST(SUM(CASE WHEN TestType = 'Behaviour' THEN CategoryPct END)
        AS decimal(5,2)) AS BehaviourScore

    FROM CategoryScore
    GROUP BY SessionID, CandidateID
),
FinalScore AS (
    SELECT
        c.SessionID,
        c.CandidateID,
        c.TechnicalScore,
        c.CognitiveScore,
        c.BehaviourScore,
        CAST(
            (b.TechnicalWeight/100 * ISNULL(TechnicalScore, 0)) +
            (b.CognitiveWeight/100 * ISNULL(CognitiveScore,  0)) +
            (b.BehaviourWeight/100 * ISNULL(BehaviourScore, 0))
        AS decimal(5,2)) AS TotalScore
    FROM TypeScore c
    INNER JOIN TestSessions a on c.SessionID=a.SessionID
    inner join TestTemplates b on a.TemplateID=b.TemplateID
)
update c
set TechnicalTestScore=fs.TechnicalScore,
CognitiveTestScore=fs.CognitiveScore,
BehaviourTestScore=fs.BehaviourScore,
TotalTestScore=fs.TotalScore
from Candidates c
inner join TestSessions ts on ts.CandidateID=c.CandidateID
inner join FinalScore fs on fs.SessionID=ts.SessionID
where fs.SessionID=@SessionID
";

                var result = await _context.Database.ExecuteSqlRawAsync(query,
                    new SqlParameter("@SessionID", sessionId));

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
