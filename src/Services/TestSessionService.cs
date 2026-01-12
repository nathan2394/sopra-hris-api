using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Entities;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using sopra_hris_api.src.Helpers;
using System.Diagnostics;

namespace sopra_hris_api.src.Services.API
{
    public class TestSessionService : IServiceAsync<TestSessions>
    {
        private readonly EFContext _context;

        public TestSessionService(EFContext context)
        {
            _context = context;
        }

        public async Task<TestSessions> CreateAsync(TestSessions data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validation: Check if a test session already exists for this candidate
                var existingSession = await _context.TestSessions
                    .FirstOrDefaultAsync(x => x.CandidateID == data.CandidateID && x.IsDeleted == false);

                if (existingSession != null)
                {
                    throw new InvalidOperationException($"A test session already exists for candidate {data.CandidateID}. Only one test session is allowed per candidate.");
                }

                await _context.TestSessions.AddAsync(data);
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

        //public async Task<TestSessionWithQuestionsResponse> GetQuestionsForSessionAsync(long sessionId)
        //{
        //    try
        //    {
        //        var session = await _context.TestSessions
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(x => x.SessionID == sessionId && x.IsDeleted == false);

        //        if (session == null)
        //            return null;

        //        var response = new TestSessionWithQuestionsResponse
        //        {
        //            SessionID = session.SessionID,
        //            CandidateID = session.CandidateID,
        //            StartTime = session.StartTime,
        //        };

        //        // Get all question categories with their total questions
        //        var categories = await _context.QuestionCategories
        //            .AsNoTracking()
        //            .Where(x => x.IsDeleted == false)
        //            .ToListAsync();

        //        var random = new Random();

        //        // For each category, get questions and answers
        //        foreach (var category in categories)
        //        {
        //            var questions = await _context.Questions
        //                .AsNoTracking()
        //                .Where(x => x.CategoryID == category.CategoryID && x.IsDeleted == false)
        //                .ToListAsync();

        //            if (questions.Count == 0)
        //                continue;

        //            // Get questions per section from QuestionCategories.TotalQuestions
        //            int questionsPerSection = category.TotalQuestions ?? 10;

        //            // Randomize questions within the section and limit
        //            var randomizedQuestions = questions.OrderBy(x => random.Next()).Take(questionsPerSection).ToList();

        //            var sectionDTO = new QuestionSectionDTO
        //            {
        //                CategoryID = category.CategoryID,
        //                CategoryName = category.CategoryName,
        //                Description = category.Description,
        //                Duration = category.Duration,
        //                TotalQuestions = category.TotalQuestions,
        //                TestType = category.TestType,
        //            };

        //            foreach (var question in randomizedQuestions)
        //            {
        //                // Get all answers for this question
        //                var answers = await _context.AnswerOptions
        //                    .AsNoTracking()
        //                    .Where(x => x.QuestionID == question.QuestionID && x.IsDeleted == false)
        //                    .ToListAsync();

        //                // Shuffle answers for this question
        //                var shuffledAnswers = answers.OrderBy(x => random.Next()).ToList();

        //                var questionDTO = new QuestionWithAnswersDTO
        //                {
        //                    QuestionID = question.QuestionID,
        //                    QuestionText = question.QuestionText,
        //                    Answers = shuffledAnswers.Select(x => new AnswerOptionDTO
        //                    {
        //                        AnswerID = x.AnswerID,
        //                        AnswerText = x.AnswerText
        //                    }).ToList()
        //                };

        //                sectionDTO.Questions.Add(questionDTO);
        //            }                                       

        //            response.Sections.Add(sectionDTO);
        //        }

        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.WriteLine(ex.Message);
        //        if (ex.StackTrace != null)
        //            Trace.WriteLine(ex.StackTrace);

        //        throw;
        //    }
        //}

        public async Task<TestSessionWithQuestionsResponse> GetQuestionsForSessionAsync(long sessionId)
        {
            try
            {
                // 1️⃣ Ambil session (validasi awal)
                var session = await _context.TestSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SessionID == sessionId && x.IsDeleted == false);

                if (session == null)
                    return null;

                // 2️⃣ Panggil Stored Procedure
                var rawData = await _context.SessionQuestionRaw
                    .FromSqlRaw(
                        "EXEC usp_GenerateSessionQuestions @SessionID",
                        new SqlParameter("@SessionID", sessionId)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                if (!rawData.Any())
                    return null;

                // 3️⃣ Build response
                var response = new TestSessionWithQuestionsResponse
                {
                    SessionID = session.SessionID,
                    CandidateID = session.CandidateID,
                    StartTime = session.StartTime
                };

                // 4️⃣ Group per Section (Category)
                var sectionGroups = rawData
                    .GroupBy(x => new { x.CategoryID, x.CategoryName, x.Duration })
                    .OrderBy(g => g.Key.CategoryID);

                foreach (var sectionGroup in sectionGroups)
                {
                    var section = new QuestionSectionDTO
                    {
                        CategoryID = sectionGroup.Key.CategoryID,
                        CategoryName = sectionGroup.Key.CategoryName,
                        Duration = sectionGroup.Key.Duration,
                        TotalQuestions = sectionGroup
                            .Select(x => x.QuestionID)
                            .Distinct()
                            .Count()
                    };

                    // 5️⃣ Group per Question (sudah random & ordered dari SQL)
                    var questionGroups = sectionGroup
                        .GroupBy(q => new { q.QuestionID, q.QuestionText, q.QuestionOrder })
                        .OrderBy(q => q.Key.QuestionOrder);

                    foreach (var questionGroup in questionGroups)
                    {
                        var question = new QuestionWithAnswersDTO
                        {
                            QuestionID = questionGroup.Key.QuestionID,
                            QuestionText = questionGroup.Key.QuestionText,
                            Answers = questionGroup
                                .Select(a => new AnswerOptionDTO
                                {
                                    AnswerID = a.AnswerID,
                                    AnswerText = a.AnswerText
                                })
                                .ToList()
                        };

                        section.Questions.Add(question);
                    }

                    response.Sections.Add(section);
                }

                return response;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
                throw;
            }
        }


        public async Task<bool> DeleteAsync(long id, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.TestSessions.FirstOrDefaultAsync(x => x.SessionID == id && x.IsDeleted == false);
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

        public async Task<TestSessions> EditAsync(TestSessions data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.TestSessions.FirstOrDefaultAsync(x => x.SessionID == data.SessionID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.CandidateID = data.CandidateID;
                obj.StartTime = data.StartTime;
                obj.EndTime = data.EndTime;
                obj.IPAddress = data.IPAddress;
                obj.UserAgent = data.UserAgent;

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


        public async Task<ListResponse<TestSessions>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.TestSessions where a.IsDeleted == false select a;

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
                    query = query.OrderByDescending(x => x.SessionID);
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

                return new ListResponse<TestSessions>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<TestSessions> GetByIdAsync(long id)
        {
            try
            {
                return await _context.TestSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<List<TestSessionDetailDTO>> GetSessionDetailsAsync(long sessionId)
        {
            try
            {
                var details = await _context.TestResponses
                    .AsNoTracking()
                    .Where(x => x.SessionID == sessionId && x.IsDeleted == false)
                    .Join(_context.TestSessions.Where(x => x.IsDeleted == false),
                        tr => tr.SessionID,
                        ts => ts.SessionID,
                        (tr, ts) => new { tr, ts })
                    .Join(_context.Candidates.Where(x => x.IsDeleted == false),
                        x => x.ts.CandidateID,
                        c => c.CandidateID,
                        (x, c) => new { x.tr, x.ts, c })
                    .Join(_context.Jobs.Where(x => x.IsDeleted == false),
                        x => x.c.JobID,
                        j => j.JobID,
                        (x, j) => new { x.tr, x.ts, x.c, j })
                    .Join(_context.Questions.Where(x => x.IsDeleted == false),
                        x => x.tr.QuestionID,
                        q => q.QuestionID,
                        (x, q) => new { x.tr, x.ts, x.c, x.j, q })
                    .Join(_context.QuestionCategories.Where(x => x.IsDeleted == false),
                        x => x.q.CategoryID,
                        qc => qc.CategoryID,
                        (x, qc) => new { x.tr, x.ts, x.c, x.j, x.q, qc })
                    .Join(_context.AnswerOptions.Where(x => x.IsDeleted == false),
                        x => x.tr.SelectedAnswerID,
                        ao => ao.AnswerID,
                        (x, ao) => new { x.tr, x.ts, x.c, x.j, x.q, x.qc, ao })
                    .Select(x => new TestSessionDetailDTO
                    {
                        CandidateID = x.c.CandidateID,
                        CandidateName = x.c.CandidateName,
                        JobID = x.j.JobID,
                        JobTitle = x.j.JobTitle,
                        SessionID = x.ts.SessionID,
                        CategoryName = x.qc.CategoryName,
                        QuestionID = x.q.QuestionID,
                        QuestionText = x.q.QuestionText,
                        SelectedAnswer = x.ao.AnswerText,
                        IsCorrect = x.ao.IsCorrect,
                        AnswerTime = x.tr.DateIn.Value
                    })
                    .OrderBy(x => x.CategoryName)
                    .ThenBy(x => x.QuestionID)
                    .ToListAsync();

                return details;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<List<TestSessionScoreByCategoryDTO>> GetSessionScoreByCategoryAsync(long sessionId)
        {
            try
            {
                var scoresByCategory = await _context.TestResponses
                    .AsNoTracking()
                    .Where(tr => tr.SessionID == sessionId && tr.IsDeleted == false)
                    .Join(_context.TestSessions.Where(ts => ts.IsDeleted == false),
                        tr => tr.SessionID,
                        ts => ts.SessionID,
                        (tr, ts) => new { tr, ts })
                    .Join(_context.Candidates.Where(c => c.IsDeleted == false),
                        x => x.ts.CandidateID,
                        c => c.CandidateID,
                        (x, c) => new { x.tr, x.ts, c })
                    .Join(_context.Jobs.Where(j => j.IsDeleted == false),
                        x => x.c.JobID,
                        j => j.JobID,
                        (x, j) => new { x.tr, x.ts, x.c, j })
                    .Join(_context.Questions.Where(q => q.IsDeleted == false),
                        x => x.tr.QuestionID,
                        q => q.QuestionID,
                        (x, q) => new { x.tr, x.ts, x.c, x.j, q })
                    .Join(_context.QuestionCategories.Where(qc => qc.IsDeleted == false),
                        x => x.q.CategoryID,
                        qc => qc.CategoryID,
                        (x, qc) => new { x.tr, x.ts, x.c, x.j, x.q, qc })
                    .Join(_context.AnswerOptions.Where(ao => ao.IsDeleted == false),
                        x => x.tr.SelectedAnswerID,
                        ao => ao.AnswerID,
                        (x, ao) => new { x.tr, x.ts, x.c, x.j, x.q, x.qc, ao })
                    .GroupBy(g => new
                    {
                        g.c.CandidateID,
                        g.c.CandidateName,
                        g.j.JobID,
                        g.j.JobTitle,
                        g.qc.CategoryID,
                        g.qc.CategoryName,
                        g.qc.TotalQuestions
                    })
                    .Select(g => new TestSessionScoreByCategoryDTO
                    {
                        CandidateID = g.Key.CandidateID,
                        CandidateName = g.Key.CandidateName,
                        JobID = g.Key.JobID,
                        JobTitle = g.Key.JobTitle,
                        CategoryName = g.Key.CategoryName,

                        // Total questions dari konfigurasi kategori (bukan dari jumlah baris response)
                        TotalQuestions = g.Key.TotalQuestions ?? 0,

                        // Soal dijawab & benar (MCQ: IsCorrect = true, SelectedAnswerID terisi)
                        CorrectAnswers = g.Count(x =>
                            x.tr.SelectedAnswerID > 0 && x.ao.IsCorrect),

                        // Salah = total soal - benar  (termasuk yang tidak dijawab)
                        WrongAnswers = (g.Key.TotalQuestions ?? 0) - g.Count(x => x.tr.SelectedAnswerID > 0 && x.ao.IsCorrect)
                    })
                    .OrderBy(x => x.CategoryName)
                    .ToListAsync();

                return scoresByCategory;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<TestSessionOverallScoreDTO> GetSessionOverallScoreAsync(long sessionId)
        {
            try
            {
                // Pakai fungsi kategori supaya logika benar/salah dan total soal konsisten
                var byCategory = await GetSessionScoreByCategoryAsync(sessionId);

                var first = byCategory.FirstOrDefault();
                if (first == null)
                    return null;

                var totalQuestions = byCategory.Sum(x => x.TotalQuestions);
                var correctAnswers = byCategory.Sum(x => x.CorrectAnswers);
                var wrongAnswers = totalQuestions - correctAnswers;

                var overall = new TestSessionOverallScoreDTO
                {
                    CandidateID = first.CandidateID,
                    CandidateName = first.CandidateName,
                    JobID = first.JobID,
                    JobTitle = first.JobTitle,
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = correctAnswers,
                    WrongAnswers = wrongAnswers
                };

                return overall;
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
