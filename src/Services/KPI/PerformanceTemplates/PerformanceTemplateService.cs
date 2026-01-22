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
    public class PerformanceTemplateService : IServicePerformanceTemplateAsync<PerformanceTemplates>
    {
        private readonly EFContext _context;

        public PerformanceTemplateService(EFContext context)
        {
            _context = context;
        }

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
                                Department = c != null ? c.Name : "",
                                Periode = a.ActiveYear,
                                TransDate = a.DateIn,
                                Status = a.Status
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Equals(search)
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
                        SELECT ID, DepartmentsID, DivisionsID, EmployeeJobTitlesID, MainValue, GeneralGoal, ActiveYear
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
                        SELECT pt.ID, pt.Name, pt.Description, pt.PerformanceTemplateDetailGroupsID, pt.PerformanceTemplatesID, pt.Weight, pt.MediaDescription, pt.Option1, pt.Option2, pt.Option3, pt.Option4, pt.Option5, pt.Approver1, pt.Approver2, pt.Approver3
                        FROM PerformanceTemplateDetails pt
                            INNER JOIN PerformanceTemplateDetailGroups ptg ON pt.PerformanceTemplateDetailGroupsID = ptg.ID
                        WHERE pt.PerformanceTemplatesID = {0}
                            AND ptg.Type = 'PP'
                            AND (pt.IsDeleted = 0 OR pt.IsDeleted IS NULL)
                    ", template.ID)
                    .AsNoTracking()
                    .ToListAsync();

                var detailPK = await _context.Set<PerformanceTemplateDetailsDto>()
                    .FromSqlRaw(@"
                        SELECT pt.ID, pt.Name, pt.Description, pt.PerformanceTemplateDetailGroupsID, pt.PerformanceTemplatesID, pt.Weight, pt.MediaDescription, pt.Option1, pt.Option2, pt.Option3, pt.Option4, pt.Option5, pt.Approver1, pt.Approver2, pt.Approver3
                        FROM PerformanceTemplateDetails pt
                            INNER JOIN PerformanceTemplateDetailGroups ptg ON pt.PerformanceTemplateDetailGroupsID = ptg.ID
                        WHERE pt.PerformanceTemplatesID = {0}
                            AND ptg.Type = 'PK'
                            AND (pt.IsDeleted = 0 OR pt.IsDeleted IS NULL)
                    ", template.ID)
                    .AsNoTracking()
                    .ToListAsync();

                var detailPM = await _context.Set<PerformanceTemplateDetailsDto>()
                    .FromSqlRaw(@"
                        SELECT pt.ID, pt.Name, pt.Description, pt.PerformanceTemplateDetailGroupsID, pt.PerformanceTemplatesID, pt.Weight, pt.MediaDescription, pt.Option1, pt.Option2, pt.Option3, pt.Option4, pt.Option5, pt.Approver1, pt.Approver2, pt.Approver3
                        FROM PerformanceTemplateDetails pt
                            INNER JOIN PerformanceTemplateDetailGroups ptg ON pt.PerformanceTemplateDetailGroupsID = ptg.ID
                        WHERE pt.PerformanceTemplatesID = {0}
                            AND ptg.Type = 'PM'
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
                        
                        INSERT INTO PerformanceTemplates (DepartmentsID, DivisionsID, EmployeeJobTitlesID, MainValue, GeneralGoal, ActiveYear, UserIn, DateIn)
                        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, GETDATE());
                        
                        SET @ID = SCOPE_IDENTITY();
                        
                        SELECT *
                        FROM PerformanceTemplates
                        WHERE ID = @ID;
                    ", data.DepartmentsID, data.DivisionsID, data.EmployeeJobTitlesID, data.MainValue ?? "", data.GeneralGoal ?? "", data.ActiveYear, userID)
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
                            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, GETDATE());
                            
                            SET @ID = SCOPE_IDENTITY();
                            
                            SELECT *
                            FROM PerformanceConditions
                            WHERE ID = @ID;
                        ", template.ID, data.Condition.AgeMin, data.Condition.AgeMax, data.Condition.ProfessionalBackground ?? "", data.Condition.EducationalBackground ?? "", data.Condition.CareerYearMin, userID)
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
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, PerformanceTemplateDetailGroupsID, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver2, Approver3, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.PerformanceTemplateDetailGroupsID, detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? "", detail.Approver2 ?? "", detail.Approver3 ?? "", userID);
                        }
                    }

                    if(data.TemplateDetails.PK != null)
                    {
                        foreach(var detail in data.TemplateDetails.PK)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, PerformanceTemplateDetailGroupsID, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver2, Approver3, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.PerformanceTemplateDetailGroupsID, detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? "", detail.Approver2 ?? "", detail.Approver3 ?? "", userID);
                        }
                    }

                    if(data.TemplateDetails.PM != null)
                    {
                        foreach(var detail in data.TemplateDetails.PM)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, PerformanceTemplateDetailGroupsID, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver2, Approver3, UserIn, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.PerformanceTemplateDetailGroupsID, detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? "", detail.Approver2 ?? "", detail.Approver3 ?? "", userID);
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
                            DepartmentsID = {0},
                            DivisionsID = {1},
                            EmployeeJobTitlesID = {2},
                            MainValue = {3},
                            GeneralGoal = {4},
                            ActiveYear = {5},
                            UserUp = {6},
                            DateUp = GETDATE()
                        WHERE ID = {7};
                        
                        SET @ID = {8};
                        
                        SELECT *
                        FROM PerformanceTemplates
                        WHERE ID = @ID;
                    ", data.DepartmentsID, data.DivisionsID, data.EmployeeJobTitlesID, data.MainValue ?? "", data.GeneralGoal ?? "", data.ActiveYear, userID, data.ID, data.ID)
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
                        ", updatedTemplate.ID, data.Condition.AgeMin, data.Condition.AgeMax, data.Condition.ProfessionalBackground ?? "", data.Condition.EducationalBackground ?? "", data.Condition.CareerYearMin, userID)
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
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, PerformanceTemplateDetailGroupsID, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver2, Approver3, UserUp, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.PerformanceTemplateDetailGroupsID, detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? "", detail.Approver2 ?? "", detail.Approver3 ?? "", userID);
                        }
                    }

                    if(data.TemplateDetails.PK != null)
                    {
                        foreach(var detail in data.TemplateDetails.PK)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, PerformanceTemplateDetailGroupsID, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver2, Approver3, UserUp, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.PerformanceTemplateDetailGroupsID, detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? "", detail.Approver2 ?? "", detail.Approver3 ?? "", userID);
                        }
                    }

                    if(data.TemplateDetails.PM != null)
                    {
                        foreach(var detail in data.TemplateDetails.PM)
                        {
                            _context.Database.ExecuteSqlRaw(@"
                                INSERT INTO PerformanceTemplateDetails (Name, Description, PerformanceTemplatesID, PerformanceTemplateDetailGroupsID, Weight, MediaDescription, Option1, Option2, Option3, Option4, Option5, Approver1, Approver2, Approver3, UserUp, DateIn)
                                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, GETDATE());
                            ", detail.Name ?? "", detail.Description ?? "", template.ID, detail.PerformanceTemplateDetailGroupsID, detail.Weight, detail.MediaDescription ?? "", detail.Option1 ?? "", detail.Option2 ?? "", detail.Option3 ?? "", detail.Option4 ?? "", detail.Option5 ?? "", detail.Approver1 ?? "", detail.Approver2 ?? "", detail.Approver3 ?? "", userID);
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
    }
}
