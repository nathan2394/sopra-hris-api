using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using Microsoft.VisualBasic;
using CsvHelper;

namespace sopra_hris_api.src.Services.API
{
    public class PerformanceEmployeeReviewerService : IServicePerformanceEmployeeReviewerAsync<PerformanceEmployeeReviewers>
    {
        private readonly EFContext _context;

        public PerformanceEmployeeReviewerService(EFContext context)
        {
            _context = context;
        }

        private sealed class OptionWeightRow
        {
            public int Option { get; set; }
            public int Weight { get; set; }
        }

        private async Task ValidateSave(ReviewerFormsDto data)
        {
            // Employee ID
            if (data.EmployeesID == 0)
            {
                throw new ArgumentException("Employee ID is required");
            }
        }

        public async Task<ListResponse<PerformanceEmployeeReviewers>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.PerformanceEmployeeReviewers
                            where a.IsDeleted == false
                            select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.EmployeesID.Equals(search)
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
                                "employeesid" => query.Where(x => x.EmployeesID.Equals(value)),
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
                            "employeesid" => query.OrderByDescending(x => x.EmployeesID),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "employeesid" => query.OrderBy(x => x.EmployeesID),
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

                return new ListResponse<PerformanceEmployeeReviewers>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ReviewerFormsDto> GetEmployeeFormByIdAsync(long userID, long reviewerID)
        {
            try
            {
                var employee = await _context.Set<Employees>()
                    .FromSqlRaw(@"
                        SELECT *
                        FROM Employees
                        WHERE EmployeeID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", userID)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if(employee == null)
                    throw new Exception("Employee not found");

                var formData = new ReviewerFormsDto
                {
                    EmployeesID = employee.EmployeeID,
                    EmployeeName = employee.EmployeeName,
                    FormDetails = new List<FormDetailsDto>()
                };

                var questions = await _context.Set<FormDetailsDto>()
                    .FromSqlRaw(@"
                        SELECT
                            per.ID, ptd.Description AS Question,
                            ptd.Option1, ptd.Option2, ptd.Option3, ptd.Option4, ptd.Option5,
                            '' AS Remarks, 0 AS SelectedOption,
                            CASE 
                                WHEN per.Approvers1ID = {1} THEN 1
                                WHEN per.Approvers2ID = {1} THEN 2
                                WHEN per.Approvers3ID = {1} THEN 3
                                WHEN per.Approvers4ID = {1} THEN 4
                                WHEN per.Approvers5ID = {1} THEN 5
                            ELSE 0
                            END AS ApproverNo
                        FROM PerformanceEmployeeReviewers per
                            INNER JOIN PerformanceTemplateDetails ptd
                            ON per.PerformanceTemplateDetailsID = ptd.ID
                                AND per.PerformanceTemplatesID = ptd.PerformanceTemplatesID
                                AND per.EmployeesID = {0}
                                AND (per.Approvers1ID = {1} OR per.Approvers2ID = {1} OR per.Approvers3ID = {1} OR per.Approvers4ID = {1} OR per.Approvers5ID = {1})
                        WHERE (per.IsDeleted = 0 OR per.IsDeleted IS NULL)
                    ", employee.EmployeeID, reviewerID)
                    .AsNoTracking()
                    .ToListAsync();

                if (questions == null)
                    throw new Exception("There is no question for this reviewer");

                formData.FormDetails = questions;

                return formData;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<List<ToBeReviewedEmployeesDto>> GetEmployeeListByIdAsync(long userID)
        {
            try
            {
                var employee = await _context.Set<Employees>()
                    .FromSqlRaw(@"
                        SELECT *
                        FROM Employees
                        WHERE EmployeeID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", userID)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if(employee == null)
                    throw new Exception("Employee not found");

                var listData = await _context.Set<ToBeReviewedEmployeesDto>()
                    .FromSqlRaw(@"
                        SELECT DISTINCT e.EmployeeID AS ID, e.EmployeeName AS Name, ejt.Name as JobTitle
                        FROM PerformanceEmployeeReviewers per
                            INNER JOIN Employees e ON per.EmployeesID = e.EmployeeID
                            INNER JOIN EmployeeJobTitles ejt ON e.JobTitleID = ejt.EmployeeJobTitleID
                        WHERE ((per.Approvers1ID = {0} AND (per.selectedOptionWeight1 = 0 OR per.selectedOptionWeight1 IS NULL)) 
                            OR (per.Approvers2ID = {0} AND (per.selectedOptionWeight2 = 0 OR per.selectedOptionWeight2 IS NULL))
                            OR (per.Approvers3ID = {0} AND (per.selectedOptionWeight3 = 0 OR per.selectedOptionWeight3 IS NULL))
                            OR (per.Approvers4ID = {0} AND (per.selectedOptionWeight4 = 0 OR per.selectedOptionWeight4 IS NULL))
                            OR (per.Approvers5ID = {0} AND (per.selectedOptionWeight5 = 0 OR per.selectedOptionWeight5 IS NULL)))
                    ", employee.EmployeeID)
                    .AsNoTracking()
                    .ToListAsync();

                return listData;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ReviewerFormsDto> EditAsync(ReviewerFormsDto data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await ValidateSave(data);

                if(data.FormDetails == null || !data.FormDetails.Any())
                    throw new Exception("Form details cannot be empty");

                var defaultWeights = new Dictionary<int, int>
                {
                    { 1, 35 },
                    { 2, 50 },
                    { 3, 70 },
                    { 4, 85 },
                    { 5, 100 }
                };

                var configuredWeights = (await _context.Database
                    .SqlQueryRaw<OptionWeightRow>(@"
                        SELECT DISTINCT [Option], [Weight]
                        FROM PerformanceOptionWeights
                    ")
                    .AsNoTracking()
                    .ToListAsync())
                    .ToDictionary(x => x.Option, x => x.Weight);

                foreach (var detail in data.FormDetails)
                {
                    var selectedOptionDescription = string.Empty;
                    var defaultWeight = 0;

                    switch (detail.SelectedOption)
                    {
                        case 1: 
                            selectedOptionDescription = detail.Option1;
                            defaultWeight = defaultWeights[1];
                            break;
                        case 2:
                            selectedOptionDescription = detail.Option2;
                            defaultWeight = defaultWeights[2];
                            break;
                        case 3:
                            selectedOptionDescription = detail.Option3;
                            defaultWeight = defaultWeights[3];
                            break;
                        case 4:
                            selectedOptionDescription = detail.Option4;
                            defaultWeight = defaultWeights[4];
                            break;
                        case 5:
                            selectedOptionDescription = detail.Option5;
                            defaultWeight = defaultWeights[5];
                            break;
                    }

                    var netWeight = configuredWeights.TryGetValue(detail.SelectedOption, out var configuredWeight)
                        ? configuredWeight
                        : defaultWeight;

                    await _context.Database.ExecuteSqlRawAsync($@"
                        UPDATE PerformanceEmployeeReviewers SET 
                            SelectedOptionDescription{detail.ApproverNo} = {{0}},
                            SelectedOptionWeight{detail.ApproverNo} = {{1}},
                            SelectedOptionNetWeight{detail.ApproverNo} = {{2}},
                            Remarks{detail.ApproverNo} = {{3}},
                            UserUp = {{4}},
                            DateUp = GETDATE()
                        WHERE EmployeesID = {{5}}
                            AND ID = {{6}}
                            AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", 
                    selectedOptionDescription,
                    detail.SelectedOption,
                    netWeight,
                    detail.Remarks ?? "",
                    userID,
                    data.EmployeesID,
                    detail.ID);

                    await _context.Database.ExecuteSqlRawAsync("EXEC usp_CalculateKPIScore");
                }

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
                var template = await _context.Set<PerformanceTemplates>()
                    .FromSqlRaw(@"
                        SELECT * FROM PerformanceTemplates
                        WHERE ID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", id)
                    .FirstOrDefaultAsync();

                if (template == null)
                    throw new Exception("There is no Performance Template with the given ID");

                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE PerformanceTemplates
                    SET IsDeleted = 1, UserUp = {0}, DateUp = GETDATE()
                    WHERE ID = {1}
                ", userID, template.ID);

                #region Delete Conditions
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE PerformanceConditions
                    SET IsDeleted = 1, UserUp = {0}, DateUp = GETDATE()
                    WHERE PerformanceTemplatesID = {1}
                ", userID, template.ID);
                #endregion

                #region Delete Trainings
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE PerformanceTrainings
                    SET IsDeleted = 1, UserUp = {0}, DateUp = GETDATE()
                    WHERE PerformanceConditionsID IN (
                        SELECT ID FROM PerformanceConditions
                        WHERE PerformanceTemplatesID = {1}
                    )
                ", userID, template.ID);
                #endregion

                #region Delete Template Details (PP, PK, PM)
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE PerformanceTemplateDetails
                    SET IsDeleted = 1, UserUp = {0}, DateUp = GETDATE()
                    WHERE PerformanceTemplatesID = {1}
                ", userID, template.ID);
                #endregion

                #region Delete Competency
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE PerformanceCompetencies
                    SET IsDeleted = 1, UserUp = {0}, DateUp = GETDATE()
                    WHERE PerformanceTemplatesID = {1}
                ", userID, template.ID);
                #endregion

                #region Delete Competency Details
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE PerformanceCompetencyDetails
                    SET IsDeleted = 1, UserUp = {0}, DateUp = GETDATE()
                    WHERE PerformanceCompetenciesID IN (
                        SELECT ID FROM PerformanceCompetencies
                        WHERE PerformanceTemplatesID = {1}
                    )
                ", userID, template.ID);
                #endregion

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