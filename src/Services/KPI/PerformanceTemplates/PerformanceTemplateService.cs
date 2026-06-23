using Microsoft.EntityFrameworkCore;
using sopra_hris_api.Helpers;
using sopra_hris_api.Responses;
using System.Diagnostics;
using sopra_hris_api.Entities;
using sopra_hris_api.src.Helpers;
using Microsoft.VisualBasic;
using CsvHelper;
using System.Security.Claims;

namespace sopra_hris_api.src.Services.API
{
    public class PerformanceTemplateService : IServicePerformanceTemplateAsync<PerformanceTemplates>
    {
        private readonly EFContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PerformanceTemplateService(EFContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        private async Task ValidateSave(PerformanceTemplatesDto data)
        {
            // Employee Job Title
            if (data.EmployeeJobTitlesID == 0)
            {
                throw new ArgumentException("Employee Job Title ID is required");
            }

            // Division & Department (Should pick one later)
            if (data.DepartmentsID == 0 && data.DivisionsID == 0)
            {
                throw new ArgumentException("Either Department ID or Division ID is required");
            }

            // Active Year .1
            if (data.ActiveYear == 0)
            {
                throw new ArgumentException("Active Year is required");
            }

            // Active Year .2
            if (data.ActiveYear < DateTime.Now.Year)
            {
                throw new ArgumentException("Active Year cannot be in the past");
            }
        }

        public async Task<ListResponse<PerformanceTemplateListDto>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.PerformanceTemplates
                            join b in _context.EmployeeJobTitles on a.EmployeeJobTitlesID equals b.EmployeeJobTitleID
                            join c in _context.Departments on a.DepartmentsID equals c.DepartmentID into deptGroup
                            from c in deptGroup.DefaultIfEmpty()
                            where a.IsDeleted == false
                            select new PerformanceTemplateListDto
                            {
                                ID = a.ID,
                                Name = b.Name,
                                DepartmentID = a.DepartmentsID,
                                Department = c != null ? c.Name : "",
                                Periode = a.ActiveYear,
                                TransDate = a.DateIn,
                                Status = a.Status
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Equals(search)
                );

                var roleID = Convert.ToInt64(User.FindFirstValue("roleid"));
                var employeeID = Convert.ToInt64(User.FindFirstValue("employeeid"));

                if (roleID != 0 && !new long[] { 1, 3, 4 }.Contains(roleID)) // Administrator & HC
                {
                    var currentEmployee = await _context.Employees
                        .FirstOrDefaultAsync(x => x.EmployeeID == employeeID);

                    if (currentEmployee != null)
                        query = query.Where(x => x.DepartmentID == currentEmployee.DepartmentID);
                }

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
                                "name" => query.Where(x => x.Name.Equals(value)),
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
                            "name" => query.OrderByDescending(x => x.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.Name),
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

                return new ListResponse<PerformanceTemplateListDto>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<PerformanceTemplatesDto> GetByIdAsync(long id)
        {
            try
            {
                var template = await _context.Set<PerformanceTemplatesDto>()
                    .FromSqlRaw(@"
                        SELECT ID, DepartmentsID, DivisionsID, EmployeeJobTitlesID, MainValue, GeneralGoal, Status, ActiveYear
                        FROM PerformanceTemplates
                        WHERE ID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", id)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (template == null)
                    throw new Exception("There is no Performance Template with the given ID");

                #region Load Condition
                var condition = await _context.Set<PerformanceConditionsDto>()
                    .FromSqlRaw(@"
                        SELECT ID, PerformanceTemplatesID, AgeMin, AgeMax, ProfessionalBackground, EducationalBackground, CareerYearMin
                        FROM PerformanceConditions
                        WHERE PerformanceTemplatesID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", template.ID)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if(condition == null)
                {
                    template.Condition = new PerformanceConditionsDto();
                }
                else
                {
                    var trainings = await _context.Set<PerformanceTrainingsDto>()
                        .FromSqlRaw(@"
                            SELECT ID, PerformanceConditionsID, Name
                            FROM PerformanceTrainings
                            WHERE PerformanceConditionsID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                        ", condition.ID)
                        .AsNoTracking()
                        .ToListAsync();

                    condition.Training = trainings;
                    template.Condition = condition;
                }
                #endregion

                #region Load TemplateDetails (PP, PK, PM)
                var detailPP = await _context.Set<PerformanceTemplateDetailsDto>()
                    .FromSqlRaw(@"
                        SELECT pt.ID, pt.Name, pt.Description, pt.CoreName, pt.Type, pt.PerformanceTemplatesID, pt.Weight, pt.MediaDescription, pt.Option1, pt.Option2, pt.Option3, pt.Option4, pt.Option5, pt.Approver1, pt.Approver1Weight, pt.Approver2, pt.Approver2Weight, pt.Approver3, pt.Approver3Weight, pt.Approver4, pt.Approver4Weight, pt.Approver5, pt.Approver5Weight,
                            (CASE WHEN ISNULL(pt.Approver1, 0) > 0 OR ISNULL(pt.Approver1Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver2, 0) > 0 OR ISNULL(pt.Approver2Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver3, 0) > 0 OR ISNULL(pt.Approver3Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver4, 0) > 0 OR ISNULL(pt.Approver4Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver5, 0) > 0 OR ISNULL(pt.Approver5Weight, 0) > 0 THEN 1 ELSE 0 END) AS CountApprover
                        FROM PerformanceTemplateDetails pt
                        WHERE pt.PerformanceTemplatesID = {0}
                            AND pt.Type = 'PP'
                            AND (pt.IsDeleted = 0 OR pt.IsDeleted IS NULL)
                    ", template.ID)
                    .AsNoTracking()
                    .ToListAsync();

                var detailPK = await _context.Set<PerformanceTemplateDetailsDto>()
                    .FromSqlRaw(@"
                        SELECT pt.ID, pt.Name, pt.Description, pt.CoreName, pt.Type, pt.PerformanceTemplatesID, pt.Weight, pt.MediaDescription, pt.Option1, pt.Option2, pt.Option3, pt.Option4, pt.Option5, pt.Approver1, pt.Approver1Weight, pt.Approver2, pt.Approver2Weight, pt.Approver3, pt.Approver3Weight, pt.Approver4, pt.Approver4Weight, pt.Approver5, pt.Approver5Weight,
                            (CASE WHEN ISNULL(pt.Approver1, 0) > 0 OR ISNULL(pt.Approver1Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver2, 0) > 0 OR ISNULL(pt.Approver2Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver3, 0) > 0 OR ISNULL(pt.Approver3Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver4, 0) > 0 OR ISNULL(pt.Approver4Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver5, 0) > 0 OR ISNULL(pt.Approver5Weight, 0) > 0 THEN 1 ELSE 0 END) AS CountApprover
                        FROM PerformanceTemplateDetails pt
                        WHERE pt.PerformanceTemplatesID = {0}
                            AND pt.Type = 'PK'
                            AND (pt.IsDeleted = 0 OR pt.IsDeleted IS NULL)
                    ", template.ID)
                    .AsNoTracking()
                    .ToListAsync();

                var detailPM = await _context.Set<PerformanceTemplateDetailsDto>()
                    .FromSqlRaw(@"
                        SELECT pt.ID, pt.Name, pt.Description, pt.CoreName, pt.Type, pt.PerformanceTemplatesID, pt.Weight, pt.MediaDescription, pt.Option1, pt.Option2, pt.Option3, pt.Option4, pt.Option5, pt.Approver1, pt.Approver1Weight, pt.Approver2, pt.Approver2Weight, pt.Approver3, pt.Approver3Weight, pt.Approver4, pt.Approver4Weight, pt.Approver5, pt.Approver5Weight,
                            (CASE WHEN ISNULL(pt.Approver1, 0) > 0 OR ISNULL(pt.Approver1Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver2, 0) > 0 OR ISNULL(pt.Approver2Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver3, 0) > 0 OR ISNULL(pt.Approver3Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver4, 0) > 0 OR ISNULL(pt.Approver4Weight, 0) > 0 THEN 1 ELSE 0 END
                            + CASE WHEN ISNULL(pt.Approver5, 0) > 0 OR ISNULL(pt.Approver5Weight, 0) > 0 THEN 1 ELSE 0 END) AS CountApprover
                        FROM PerformanceTemplateDetails pt
                        WHERE pt.PerformanceTemplatesID = {0}
                            AND pt.Type = 'PM'
                            AND (pt.IsDeleted = 0 OR pt.IsDeleted IS NULL)
                    ", template.ID)
                    .AsNoTracking()
                    .ToListAsync();

                var templateDetails = new PerformanceTemplateDetailSectionDto
                {
                    PP = detailPP,
                    PK = detailPK,
                    PM = detailPM                    
                };

                template.TemplateDetails = templateDetails;
                #endregion

                #region Load Competency
                var competencies = await _context.Set<PerformanceCompetenciesDto>()
                    .FromSqlRaw(@"
                        SELECT ID, PerformanceTemplatesID, Name
                        FROM PerformanceCompetencies
                        WHERE PerformanceTemplatesID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", template.ID)
                    .AsNoTracking()
                    .ToListAsync();

                if(competencies == null)
                {
                    template.Competency = new List<PerformanceCompetenciesDto>();
                }
                else
                {
                    foreach(var competency in competencies)
                    {
                        var competencyDetails = await _context.Set<PerformanceCompetencyDetailsDto>()
                            .FromSqlRaw(@"
                                SELECT ID, PerformanceCompetenciesID, Description
                                FROM PerformanceCompetencyDetails
                                WHERE PerformanceCompetenciesID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                            ", competency.ID)
                            .AsNoTracking()
                            .ToListAsync();

                        if(competencyDetails == null)
                        {
                            competency.CompetencyDetails = new List<PerformanceCompetencyDetailsDto>();
                        }
                        else
                        {
                            competency.CompetencyDetails = competencyDetails;
                        }
                    }

                    template.Competency = competencies;
                }
                #endregion

                return template;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<PerformanceTemplates> CreateAsync(PerformanceTemplatesDto data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await ValidateSave(data);

                var template = _context.Set<PerformanceTemplates>()
                    .FromSqlRaw(@"
                        DECLARE @ID INT;
                        
                        INSERT INTO PerformanceTemplates (DepartmentsID, DivisionsID, EmployeeJobTitlesID, MainValue, GeneralGoal, Status, ActiveYear, UserIn, DateIn)
                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, GETDATE());
                        
                        SET @ID = SCOPE_IDENTITY();
                        
                        SELECT *
                        FROM PerformanceTemplates
                        WHERE ID = @ID;
                    ", data.DepartmentsID, data.DivisionsID, data.EmployeeJobTitlesID, data.MainValue ?? "", data.GeneralGoal ?? "", data.Status, data.ActiveYear, userID)
                    .AsEnumerable()
                    .FirstOrDefault();

                if(template == null)
                    throw new Exception("Failed to create Performance Template");

                #region Insert Conditions
                if(data.Condition != null)
                {
                    var condition = _context.Set<PerformanceConditions>()
                        .FromSqlRaw(@"
                            DECLARE @ID INT;
                            
                            INSERT INTO PerformanceConditions (PerformanceTemplatesID, AgeMin, AgeMax, ProfessionalBackground, EducationalBackground, CareerYearMin, UserIn, DateIn)
                            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, GETDATE());
                            
                            SET @ID = SCOPE_IDENTITY();
                            
                            SELECT *
                            FROM PerformanceConditions
                            WHERE ID = @ID;
                        ", template.ID, data.Condition.AgeMin ?? 0, data.Condition.AgeMax ?? 0, data.Condition.ProfessionalBackground ?? "", data.Condition.EducationalBackground ?? "", data.Condition.CareerYearMin ?? 0, userID)
                        .AsEnumerable()
                        .FirstOrDefault();

                    if(condition != null && data.Condition.Training != null)
                    {
                        foreach(var training in data.Condition.Training)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTrainings (PerformanceConditionsID, Name, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, GETDATE());
                            ", condition.ID, training.Name ?? "", userID);
                        }
                    }
                }
                #endregion

                #region Insert TemplateDetails (PP, PK, PM)
                if(data.TemplateDetails != null)
                {
                    if(data.TemplateDetails.PP != null)
                    {
                        foreach(var detail in data.TemplateDetails.PP)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, CoreName, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver1Weight, Approver2, Approver2Weight, Approver3, Approver3Weight, Approver4, Approver4Weight, Approver5, Approver5Weight, Type, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, 'PP', {21}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.CoreName ?? "", detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? 0, detail.Approver1Weight, detail.Approver2 ?? 0, detail.Approver2Weight, detail.Approver3 ?? 0, detail.Approver3Weight, detail.Approver4 ?? 0, detail.Approver4Weight, detail.Approver5 ?? 0, detail.Approver5Weight, userID);
                        }
                    }

                    if(data.TemplateDetails.PK != null)
                    {
                        foreach(var detail in data.TemplateDetails.PK)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, CoreName, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver1Weight, Approver2, Approver2Weight, Approver3, Approver3Weight, Approver4, Approver4Weight, Approver5, Approver5Weight, Type, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, 'PK', {21}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.CoreName ?? "", detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? 0, detail.Approver1Weight, detail.Approver2 ?? 0, detail.Approver2Weight, detail.Approver3 ?? 0, detail.Approver3Weight, detail.Approver4 ?? 0, detail.Approver4Weight, detail.Approver5 ?? 0, detail.Approver5Weight, userID);
                        }
                    }

                    if(data.TemplateDetails.PM != null)
                    {
                        foreach(var detail in data.TemplateDetails.PM)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, CoreName, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver1Weight, Approver2, Approver2Weight, Approver3, Approver3Weight, Approver4, Approver4Weight, Approver5, Approver5Weight, Type, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, 'PM', {21}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.CoreName ?? "", detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? 0, detail.Approver1Weight, detail.Approver2 ?? 0, detail.Approver2Weight, detail.Approver3 ?? 0, detail.Approver3Weight, detail.Approver4 ?? 0, detail.Approver4Weight, detail.Approver5 ?? 0, detail.Approver5Weight, userID);
                        }
                    }
                }
                #endregion

                #region Insert Competency
                if(data.Competency != null)
                {
                    foreach(var competency in data.Competency)
                    {
                        var comp = _context.Set<PerformanceCompetencies>()
                            .FromSqlRaw(@"
                                DECLARE @ID INT;
                                
                                INSERT INTO PerformanceCompetencies (PerformanceTemplatesID, Name, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, GETDATE());
                                
                                SET @ID = SCOPE_IDENTITY();
                                
                                SELECT *
                                FROM PerformanceCompetencies
                                WHERE ID = @ID;
                            ", template.ID, competency.Name ?? "", userID)
                            .AsEnumerable()
                            .FirstOrDefault();

                        if(comp != null && competency.CompetencyDetails != null)
                        {
                            foreach(var detail in competency.CompetencyDetails)
                            {
                                _context.Database.ExecuteSqlRaw(@"
                                    INSERT INTO PerformanceCompetencyDetails (PerformanceCompetenciesID, Description, UserIn, DateIn)
                                    VALUES ({0}, {1}, {2}, GETDATE());
                                ", comp.ID, detail.Description ?? "", userID);
                            }
                        }
                    }
                }
                #endregion
                
                if (data.Status == true)
                    await PublishAsync(template.ID, userID);

                await dbTrans.CommitAsync();

                return template;
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

        public async Task<PerformanceTemplates> EditAsync(PerformanceTemplatesDto data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await ValidateSave(data);

                var template = await _context.Set<PerformanceTemplatesDto>()
                    .FromSqlRaw(@"
                        SELECT *
                        FROM PerformanceTemplates
                        WHERE ID = {0} AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", data.ID)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (template == null)
                    throw new Exception("There is no Performance Template with the given ID");

                var updatedTemplate = _context.Set<PerformanceTemplates>()
                    .FromSqlRaw(@"
                        DECLARE @ID INT;
                        
                        UPDATE PerformanceTemplates SET
                            DepartmentsID = {1},
                            DivisionsID = {2},
                            EmployeeJobTitlesID = {3},
                            MainValue = {4},
                            GeneralGoal = {5},
                            ActiveYear = {6},
                            UserUp = {7},
                            DateUp = GETDATE()
                        WHERE ID = {0};
                        
                        SET @ID = {0};
                        
                        SELECT *
                        FROM PerformanceTemplates
                        WHERE ID = @ID;
                    ", data.ID, data.DepartmentsID, data.DivisionsID, data.EmployeeJobTitlesID, data.MainValue ?? "", data.GeneralGoal ?? "", data.ActiveYear, userID)
                    .AsEnumerable()
                    .FirstOrDefault();

                if (updatedTemplate == null)
                    throw new Exception("Failed to update Performance Template");

                #region Update Conditions
                if(data.Condition != null)
                {
                    var condition = _context.Set<PerformanceConditions>()
                        .FromSqlRaw(@"
                            UPDATE PerformanceConditions SET
                                AgeMin = {1},
                                AgeMax = {2},
                                ProfessionalBackground = {3},
                                EducationalBackground = {4},
                                CareerYearMin = {5},
                                UserUp = {6},
                                DateUp = GETDATE()
                            WHERE PerformanceTemplatesID = {0};
                            
                            SELECT *
                            FROM PerformanceConditions
                            WHERE PerformanceTemplatesID = {0};
                        ", updatedTemplate.ID, data.Condition.AgeMin ?? 0, data.Condition.AgeMax ?? 0, data.Condition.ProfessionalBackground ?? "", data.Condition.EducationalBackground ?? "", data.Condition.CareerYearMin ?? 0, userID)
                        .AsEnumerable()
                        .FirstOrDefault();
                    
                    if (condition == null)
                        throw new Exception("There is no Performance Condition with the given ID");

                    await _context.Database.ExecuteSqlRawAsync(@"
                        DELETE FROM PerformanceTrainings
                        WHERE PerformanceConditionsID = {0};
                    ", condition.ID);

                    if (data.Condition.Training != null)
                    {
                        foreach (var training in data.Condition.Training)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTrainings (PerformanceConditionsID, Name, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, GETDATE());
                            ", condition.ID, training.Name ?? "", userID);
                        }
                    }
                }
                #endregion

                #region Update TemplateDetails (PP, PK, PM)
                await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM PerformanceTemplateDetails
                    WHERE PerformanceTemplatesID = {0};
                ", updatedTemplate.ID);

                if(data.TemplateDetails != null)
                {   
                    if(data.TemplateDetails.PP != null)
                    {
                        foreach(var detail in data.TemplateDetails.PP)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, CoreName, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver1Weight, Approver2, Approver2Weight, Approver3, Approver3Weight, Approver4, Approver4Weight, Approver5, Approver5Weight, Type, UserUp, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, 'PP', {21}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.CoreName ?? "", detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? 0, detail.Approver1Weight, detail.Approver2 ?? 0, detail.Approver2Weight, detail.Approver3 ?? 0, detail.Approver3Weight, detail.Approver4 ?? 0, detail.Approver4Weight, detail.Approver5 ?? 0, detail.Approver5Weight, userID);
                        }
                    }

                    if(data.TemplateDetails.PK != null)
                    {
                        foreach(var detail in data.TemplateDetails.PK)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, CoreName, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver1Weight, Approver2, Approver2Weight, Approver3, Approver3Weight, Approver4, Approver4Weight, Approver5, Approver5Weight, Type, UserUp, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, 'PK', {21}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.CoreName ?? "", detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? 0, detail.Approver1Weight, detail.Approver2 ?? 0, detail.Approver2Weight, detail.Approver3 ?? 0, detail.Approver3Weight, detail.Approver4 ?? 0, detail.Approver4Weight, detail.Approver5 ?? 0, detail.Approver5Weight, userID);
                        }
                    }

                    if(data.TemplateDetails.PM != null)
                    {
                        foreach(var detail in data.TemplateDetails.PM)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, CoreName, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver1Weight, Approver2, Approver2Weight, Approver3, Approver3Weight, Approver4, Approver4Weight, Approver5, Approver5Weight, Type, UserUp, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, 'PM', {21}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.CoreName ?? "", detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? 0, detail.Approver1Weight, detail.Approver2 ?? 0, detail.Approver2Weight, detail.Approver3 ?? 0, detail.Approver3Weight, detail.Approver4 ?? 0, detail.Approver4Weight, detail.Approver5 ?? 0, detail.Approver5Weight, userID);
                        }
                    }
                }
                #endregion

                #region Update Competency
                await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE pcd
                    FROM PerformanceCompetencyDetails pcd
                        INNER JOIN PerformanceCompetencies pc ON pcd.PerformanceCompetenciesID = pc.ID
                    WHERE pc.PerformanceTemplatesID = {0};
                ", updatedTemplate.ID);

                await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM PerformanceCompetencies
                    WHERE PerformanceTemplatesID = {0};
                ", updatedTemplate.ID);

                if(data.Competency != null)
                {
                    foreach(var competency in data.Competency)
                    {
                        var comp = _context.Set<PerformanceCompetencies>()
                            .FromSqlRaw(@"
                                DECLARE @ID INT;
                                
                                INSERT INTO PerformanceCompetencies (PerformanceTemplatesID, Name, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, GETDATE());
                                
                                SET @ID = SCOPE_IDENTITY();
                                
                                SELECT *
                                FROM PerformanceCompetencies
                                WHERE ID = @ID;
                            ", updatedTemplate.ID, competency.Name ?? "", userID)
                            .AsEnumerable()
                            .FirstOrDefault();

                        if(comp != null && competency.CompetencyDetails != null)
                        {
                            foreach(var detail in competency.CompetencyDetails)
                            {
                                _context.Database.ExecuteSqlRaw(@"
                                    INSERT INTO PerformanceCompetencyDetails (PerformanceCompetenciesID, Description, UserIn, DateIn)
                                    VALUES ({0}, {1}, {2}, GETDATE());
                                ", comp.ID, detail.Description ?? "", userID);
                            }
                        }
                    }
                }
                #endregion

                await dbTrans.CommitAsync();

                return updatedTemplate;
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

        public async Task<PerformanceTemplatesDto> PublishAsync(long id, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var template = await _context.Set<PerformanceTemplates>()
                    .FromSqlRaw(@"
                        SELECT *
                        FROM PerformanceTemplates
                        WHERE ID = {0}
                            AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", id)
                    .FirstOrDefaultAsync();

                if (template == null)
                    throw new Exception("There is no Performance Template with the given ID");

                var certainEmployees = await _context.Database
                    .SqlQuery<long>($@"
                        SELECT CAST(EmployeeID AS BIGINT) AS Value
                        FROM Employees
                        WHERE DepartmentID = {template.DepartmentsID}
                            AND JobTitleID = {template.EmployeeJobTitlesID}
                            AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ")
                    .ToListAsync();

                if (certainEmployees.Count == 0)
                    throw new Exception("At least one employee must match the Department and Job Title of the Performance Template.");

                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE PerformanceTemplates
                    SET
                        Status = 1,
                        UserUp = {0},
                        DateUp = GETDATE()
                    WHERE ID = {1}
                ", userID, template.ID);

                await _context.Database.ExecuteSqlRawAsync(@"
                    WITH ApprovalTemplate AS (
                        SELECT
                            A.ID, A.DepartmentsID, A.EmployeeJobTitlesID, B.ID AS TemplateID, B.Name,
                            MAX(CASE WHEN V.ApproverNum = 1 THEN C.Name END) AS Approvers1Category,
                            MAX(CASE WHEN V.ApproverNum = 2 THEN C.Name END) AS Approvers2Category,
                            MAX(CASE WHEN V.ApproverNum = 3 THEN C.Name END) AS Approvers3Category,
                            MAX(CASE WHEN V.ApproverNum = 4 THEN C.Name END) AS Approvers4Category,
                            MAX(CASE WHEN V.ApproverNum = 5 THEN C.Name END) AS Approvers5Category
                        FROM PerformanceTemplates A
                            INNER JOIN PerformanceTemplateDetails B ON A.ID = B.PerformanceTemplatesID
                            CROSS APPLY (VALUES 
                                (1, B.Approver1), (2, B.Approver2), (3, B.Approver3),
                                (4, B.Approver4), (5, B.Approver5)
                            ) AS V(ApproverNum, ApproverID)
                            LEFT JOIN PerformanceApproverCategories C ON V.ApproverID = C.ID
                        WHERE A.Status = 1
                            AND A.ID = {0}
                        GROUP BY A.ID, A.DepartmentsID, A.EmployeeJobTitlesID, B.ID, B.Name
                    )

                    INSERT INTO PerformanceEmployeeApprovals
                        (PerformanceTemplatesID, PerformanceTemplateDetailsID, SubCore, EmployeeID, Approvers1Category, Approvers2Category, Approvers3Category, Approvers4Category, Approvers5Category, UserIn, DateIn)
                    SELECT
                        at.ID, at.TemplateID, at.Name, e.EmployeeID,
                        at.Approvers1Category, at.Approvers2Category, at.Approvers3Category, at.Approvers4Category, at.Approvers5Category,
                        {0}, GETDATE()
                    FROM ApprovalTemplate at
                        CROSS JOIN Employees e
                    WHERE e.DepartmentID = at.DepartmentsID
                        AND e.JobTitleID = at.EmployeeJobTitlesID
                        AND (e.IsDeleted = 0 OR e.IsDeleted IS NULL)
                ", template.ID, userID);

                var publishedTemplate = await _context.Set<PerformanceTemplatesDto>()
                    .FromSqlRaw(@"
                        SELECT *
                        FROM PerformanceTemplates
                        WHERE ID = {0}
                            AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ", id)
                    .FirstOrDefaultAsync();

                await dbTrans.CommitAsync();

                return publishedTemplate;
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

        public async Task<ListResponse<PerformanceEmployeeApprovalsListDto>> GetReviewerAssignListAsync(int limit, int page)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                
                var query = from pt in _context.PerformanceTemplates
                            join ejt in _context.EmployeeJobTitles on pt.EmployeeJobTitlesID equals ejt.EmployeeJobTitleID
                            where pt.Status == true && (pt.IsDeleted == false || pt.IsDeleted == null)
                            select new
                            {
                                pt.ID,
                                TemplateName = ejt.Name,
                                pt.ActiveYear,
                                DepartmentID = pt.DepartmentsID
                            };

                var roleID = Convert.ToInt64(User.FindFirstValue("roleid"));
                var employeeID = Convert.ToInt64(User.FindFirstValue("employeeid"));

                if (roleID != 0 && !new long[] { 1, 3, 4 }.Contains(roleID)) // Administrator & HC
                {
                    var currentEmployee = await _context.Employees
                        .FirstOrDefaultAsync(x => x.EmployeeID == employeeID);

                    if (currentEmployee != null)
                        query = query.Where(x => x.DepartmentID == currentEmployee.DepartmentID);
                }

                var result = new List<PerformanceEmployeeApprovalsListDto>();
                
                foreach (var template in await query.ToListAsync())
                {
                    var totalEmp = await _context.Set<PerformanceEmployeeApprovals>()
                        .Where(x => x.PerformanceTemplatesID == template.ID && (x.IsDeleted == false || x.IsDeleted == null))
                        .Select(x => x.EmployeeID)
                        .Distinct()
                        .CountAsync();

                    var assignedEmp = await _context.Set<PerformanceEmployeeApprovals>()
                        .Where(x => x.PerformanceTemplatesID == template.ID 
                            && (x.IsDeleted == false || x.IsDeleted == null)
                            && ((x.Approvers1ID ?? 0) > 0 || (x.Approvers2ID ?? 0) > 0 || (x.Approvers3ID ?? 0) > 0 || (x.Approvers4ID ?? 0) > 0 || (x.Approvers5ID ?? 0) > 0))
                        .Select(x => x.EmployeeID)
                        .Distinct()
                        .CountAsync();

                    result.Add(new PerformanceEmployeeApprovalsListDto
                    {
                        TemplateID = template.ID,
                        TemplateName = template.TemplateName,
                        ActiveYear = template.ActiveYear,
                        TotalEmployees = totalEmp,
                        AssignedEmployees = assignedEmp,
                        UnassignedEmployees = totalEmp - assignedEmp
                    });
                }

                var total = result.Count;

                if (limit != 0)
                    result = result.Skip(page * limit).Take(limit).ToList();

                return new ListResponse<PerformanceEmployeeApprovalsListDto>(result, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<PerformanceEmployeeApprovalsDetailDto> GetReviewerAssignDetailAsync(long templateId)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var template = await (from pt in _context.PerformanceTemplates
                                      join ejt in _context.EmployeeJobTitles on pt.EmployeeJobTitlesID equals ejt.EmployeeJobTitleID
                                      where pt.ID == templateId && (pt.IsDeleted == false || pt.IsDeleted == null)
                                      select new
                                      {
                                            pt.ID,
                                            TemplateName = ejt.Name,
                                            EmployeeJobTitleID = pt.EmployeeJobTitlesID
                                      }).FirstOrDefaultAsync();

                if (template == null)
                    throw new Exception("Performance template not found");

                var employeeIds = await _context.Set<PerformanceEmployeeApprovals>()
                    .Where(x => x.PerformanceTemplatesID == templateId && (x.IsDeleted == false || x.IsDeleted == null))
                    .Select(x => x.EmployeeID)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

                var isReviewed = await _context.Set<PerformanceEmployeeReviewers>()
                    .Where(x => x.PerformanceTemplatesID == templateId
                        && (x.IsDeleted == false || x.IsDeleted == null)
                        && x.TotalWeight > 0)
                    .AnyAsync();

                var employees = new List<PerformanceEmployeeApprovalsEmployeeDetailDto>();
                foreach (var employeeId in employeeIds)
                {
                    employees.Add(await GetEmployeeReviewerDetailAsync(templateId, employeeId));
                }

                return new PerformanceEmployeeApprovalsDetailDto
                {
                    TemplateID = templateId,
                    TemplateName = template.TemplateName,
                    EmployeeJobTitleID = template.EmployeeJobTitleID,
                    IsReviewed = isReviewed,
                    Employees = employees
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<PerformanceEmployeeApprovalsDetailDto> AssignReviewerAsync(AssignReviewerPayloadDto data, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                if (data.Employees == null || data.Employees.Count == 0)
                    throw new Exception("No employee assignments provided");

                foreach (var employee in data.Employees)
                {
                    if (employee.DetailAssignments == null || employee.DetailAssignments.Count == 0)
                        throw new Exception($"No detail assignments provided for employee {employee.EmployeeID}");

                    foreach (var detail in employee.DetailAssignments)
                    {
                        var approval = await _context.Set<PerformanceEmployeeApprovals>()
                            .FromSqlRaw(@"
                                SELECT *
                                FROM PerformanceEmployeeApprovals
                                WHERE PerformanceTemplatesID = {0}
                                    AND PerformanceTemplateDetailsID = {1}
                                    AND EmployeeID = {2}
                                    AND (IsDeleted = 0 OR IsDeleted IS NULL)
                            ", data.TemplateID, detail.DetailID, employee.EmployeeID)
                            .AsNoTracking()
                            .FirstOrDefaultAsync();

                        if (approval == null)
                            throw new Exception($"Performance Employee Approval record not found for detail {detail.DetailID} and employee {employee.EmployeeID}");

                        await _context.Database.ExecuteSqlRawAsync(@"
                            UPDATE PerformanceEmployeeApprovals
                            SET
                                Approvers1ID = {3},
                                Approvers2ID = {4},
                                Approvers3ID = {5},
                                Approvers4ID = {6},
                                Approvers5ID = {7},
                                UserUp = {8},
                                DateUp = GETDATE()
                            WHERE PerformanceTemplatesID = {0}
                                AND PerformanceTemplateDetailsID = {1}
                                AND EmployeeID = {2}
                        ", data.TemplateID, detail.DetailID, employee.EmployeeID,
                        detail.Approvers1ID ?? 0, detail.Approvers2ID ?? 0, detail.Approvers3ID ?? 0,
                        detail.Approvers4ID ?? 0, detail.Approvers5ID ?? 0, userID);

                        await _context.Database.ExecuteSqlRawAsync(@"
                            DELETE FROM PerformanceEmployeeReviewers
                            WHERE PerformanceTemplatesID = {0}
                                AND PerformanceTemplateDetailsID = {1}
                                AND EmployeesID = {2}
                        ", data.TemplateID, detail.DetailID, employee.EmployeeID);

                        var templateDetails = await _context.Set<PerformanceTemplateDetails>()
                            .FromSqlRaw(@"
                                SELECT *
                                FROM PerformanceTemplateDetails
                                WHERE ID = {0}
                                    AND (IsDeleted = 0 OR IsDeleted IS NULL)
                            ", detail.DetailID)
                            .AsNoTracking()
                            .FirstOrDefaultAsync();

                        await _context.Database.ExecuteSqlRawAsync(@"
                            INSERT INTO PerformanceEmployeeReviewers 
                                (PerformanceTemplatesID, PerformanceTemplateDetailsID, EmployeesID, Approvers1ID, Approvers2ID, Approvers3ID, Approvers4ID, Approvers5ID, Option1, Option2, Option3, Option4, Option5, TotalWeight, UserIn, DateIn)
                            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, GETDATE())
                        ", data.TemplateID, detail.DetailID, employee.EmployeeID,
                        detail.Approvers1ID ?? 0, detail.Approvers2ID ?? 0, detail.Approvers3ID ?? 0,
                        detail.Approvers4ID ?? 0, detail.Approvers5ID ?? 0,
                        templateDetails.Option1 ?? "", templateDetails.Option2 ?? "", templateDetails.Option3 ?? "", templateDetails.Option4 ?? "", templateDetails.Option5 ?? "", 0, userID);
                    }
                }

                var result = await GetReviewerAssignDetailAsync(data.TemplateID);

                await dbTrans.CommitAsync();

                return result;
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

        private async Task<PerformanceEmployeeApprovalsEmployeeDetailDto> GetEmployeeReviewerDetailAsync(long templateId, long employeeId)
        {
            var employee = await _context.Employees
                .Where(e => e.EmployeeID == employeeId)
                .Select(e => new { e.EmployeeID, e.EmployeeName, e.JobTitleID, e.DepartmentID })
                .FirstOrDefaultAsync();

            if (employee == null)
                throw new Exception("Employee not found");

            var jobTitle = await _context.EmployeeJobTitles.Where(jt => jt.EmployeeJobTitleID == employee.JobTitleID).Select(jt => jt.Name).FirstOrDefaultAsync();
            var department = await _context.Departments.Where(d => d.DepartmentID == employee.DepartmentID).Select(d => d.Name).FirstOrDefaultAsync();

            var subcores = await _context.Set<SubcoreApprovalDetailDto>()
                .FromSqlRaw(@"
                    SELECT 
                        ptd.ID as DetailID,
                        ptd.Name as SubcoreName,
                        ISNULL(pea.Approvers1ID, 0) as Approvers1ID,
                        pea.Approvers1Category as Approvers1Category,
                        ptd.Approver1Weight as Approvers1Weight,
                        ISNULL((SELECT EmployeeName FROM Employees WHERE EmployeeID = pea.Approvers1ID), '') as Approvers1Name,
                        ISNULL(pea.Approvers2ID, 0) as Approvers2ID,
                        pea.Approvers2Category as Approvers2Category,
                        ptd.Approver2Weight as Approvers2Weight,
                        ISNULL((SELECT EmployeeName FROM Employees WHERE EmployeeID = pea.Approvers2ID), '') as Approvers2Name,
                        ISNULL(pea.Approvers3ID, 0) as Approvers3ID,
                        pea.Approvers3Category as Approvers3Category,
                        ptd.Approver3Weight as Approvers3Weight,
                        ISNULL((SELECT EmployeeName FROM Employees WHERE EmployeeID = pea.Approvers3ID), '') as Approvers3Name,
                        ISNULL(pea.Approvers4ID, 0) as Approvers4ID,
                        pea.Approvers4Category as Approvers4Category,
                        ptd.Approver4Weight as Approvers4Weight,
                        ISNULL((SELECT EmployeeName FROM Employees WHERE EmployeeID = pea.Approvers4ID), '') as Approvers4Name,
                        ISNULL(pea.Approvers5ID, 0) as Approvers5ID,
                        pea.Approvers5Category as Approvers5Category,
                        ptd.Approver5Weight as Approvers5Weight,
                        ISNULL((SELECT EmployeeName FROM Employees WHERE EmployeeID = pea.Approvers5ID), '') as Approvers5Name
                    FROM PerformanceTemplateDetails ptd
                    LEFT JOIN PerformanceEmployeeApprovals pea ON ptd.ID = pea.PerformanceTemplateDetailsID AND pea.EmployeeID = {1}
                    WHERE ptd.PerformanceTemplatesID = {0} AND (ptd.IsDeleted = 0 OR ptd.IsDeleted IS NULL)
                ", templateId, employeeId)
                .AsNoTracking()
                .ToListAsync();

            return new PerformanceEmployeeApprovalsEmployeeDetailDto
            {
                EmployeeID = employee.EmployeeID,
                EmployeeName = employee.EmployeeName,
                JobTitle = jobTitle,
                Department = department,
                SubcoreDetails = subcores
            };
        }

        public async Task<PerformanceEmployeeReviewerMatrix> GetDefaultReviewerMatrixAsync(long employeeJobTitleId)
        {
            var matrix = await _context.Set<PerformanceEmployeeReviewerMatrix>()
                .FromSqlRaw(@"
                    SELECT *
                    FROM PerformanceEmployeeReviewerMatrix
                    WHERE EmployeeJobTitleID = {0}
                ", employeeJobTitleId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (matrix == null)
                throw new Exception("Employee Reviewer Matrix not found for the given Job Title ID");

            return matrix;
        }
    }
}
