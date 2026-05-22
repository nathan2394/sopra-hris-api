USE [SOPRA_HRIS];
SET XACT_ABORT ON;

BEGIN TRAN;

IF OBJECT_ID('dbo.Company', 'U') IS NOT NULL
BEGIN
	DROP TABLE Company
	
	SELECT *
    INTO dbo.Company
    FROM SOPRA_HRIS_DEV..Company
END;

IF COL_LENGTH('dbo.Company', 'Code') IS NULL
BEGIN
    ALTER TABLE Company
	ADD Code VARCHAR(255)
END;

IF COL_LENGTH('dbo.Company', 'ApiLink') IS NULL
BEGIN
    ALTER TABLE Company
	ADD ApiLink VARCHAR(255)
END;

IF COL_LENGTH('dbo.Company', 'LogoPath') IS NULL
BEGIN
    ALTER TABLE Company
	ADD LogoPath VARCHAR(255)
END;

IF OBJECT_ID('dbo.UserCompanies', 'V') IS NULL
BEGIN
    EXEC('
        CREATE VIEW dbo.UserCompanies
		AS
		SELECT
			u.*, c.Code, c.Name AS Company, c.ApiLink, c.LogoPath
		FROM SOPRA_HRIS..Users u
			INNER JOIN SOPRA_HRIS..Company c ON c.Code = ''SOPRA''
		    WHERE U.IsDeleted != 1
		UNION ALL
		SELECT
			u.*, c.Code, c.Name AS Company, c.ApiLink, c.LogoPath
		FROM TRASS_HRIS..Users u
			INNER JOIN SOPRA_HRIS..Company c ON c.Code = ''TRASS''
		    WHERE U.IsDeleted != 1
		UNION ALL
		SELECT
			u.*, c.Code, c.Name AS Company, c.ApiLink, c.LogoPath
		FROM TRASMI_HRIS..Users u
			INNER JOIN SOPRA_HRIS..Company c ON c.Code = ''TRASMI''
		    WHERE U.IsDeleted != 1;
    ');
END;

IF OBJECT_ID('dbo.PerformanceApproverCategories', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceApproverCategories (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		Name VARCHAR(255),
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceLevel', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceLevel (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		Level INT,
		PP INT, -- Max percentage for PP Section
		PK INT, -- Max percentage for PK Section
		PM INT, -- Max percentage for PM Section
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceTemplates', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceTemplates (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		DepartmentsID BIGINT,
		DivisionsID BIGINT,
		EmployeeJobTitlesID BIGINT,
		MainValue VARCHAR(255),
		GeneralGoal VARCHAR(255),
		Status BIT DEFAULT 0, -- 0 = Draft, 1 = Published
		ActiveYear INT,
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceTemplateDetails', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceTemplateDetails (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		CoreName VARCHAR(255),
		Name VARCHAR(255),
		Description TEXT,
		PerformanceTemplatesID BIGINT,
		Type VARCHAR(2),
		Weight DECIMAL(5,2),
		MediaDescription VARCHAR(255),
		Option1 TEXT,
		Option2 TEXT,
		Option3 TEXT,
		Option4 TEXT,
		Option5 TEXT,
		Approver1 BIGINT,
		Approver1Weight INT,
		Approver2 BIGINT,
		Approver2Weight INT,
		Approver3 BIGINT,
		Approver3Weight INT,
		Approver4 BIGINT,
		Approver4Weight INT,
		Approver5 BIGINT,
		Approver5Weight INT,
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceEmployeeReviewers', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceEmployeeReviewers (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		PerformanceTemplatesID BIGINT,
		PerformanceTemplateDetailsID BIGINT,
		EmployeesID BIGINT,
		Approvers1ID BIGINT,
		Approvers2ID BIGINT,
		Approvers3ID BIGINT,
		Approvers4ID BIGINT,
		Approvers5ID BIGINT,
		Option1 TEXT,
		Option2 TEXT,
		Option3 TEXT,
		Option4 TEXT,
		Option5 TEXT,
		TotalWeight DECIMAL(3,2),
		SelectedOption1 INT,
		SelectedOptionDescription1 TEXT,
		SelectedOptionWeight1 INT,
		SelectedOptionNetWeight1 DECIMAL(3,2),
		SelectedOption2 INT,
		SelectedOptionDescription2 TEXT,
		SelectedOptionWeight2 INT,
		SelectedOptionNetWeight2 DECIMAL(3,2),
		SelectedOption3 INT,
		SelectedOptionDescription3 TEXT,
		SelectedOptionWeight3 INT,
		SelectedOptionNetWeight3 DECIMAL(3,2),
		SelectedOption4 INT,
		SelectedOptionDescription4 TEXT,
		SelectedOptionWeight4 INT,
		SelectedOptionNetWeight4 DECIMAL(3,2),
		selectedOption5 INT,
		SelectedOptionDescription5 TEXT,
		SelectedOptionWeight5 INT,
		SelectedOptionNetWeight5 DECIMAL(3,2),
		Remarks1 VARCHAR(255),
		Remarks2 VARCHAR(255),
		Remarks3 VARCHAR(255),
		Remarks4 VARCHAR(255),
		Remarks5 VARCHAR(255),
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceEmployeeApprovals', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceEmployeeApprovals (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		PerformanceTemplatesID BIGINT,
		PerformanceTemplateDetailsID BIGINT,
		SubCore VARCHAR(255),
		Approvers1Category VARCHAR(255),
		Approvers1Name VARCHAR(255),
		Approvers1ID BIGINT,
		Approvers2Category VARCHAR(255),
		Approvers2Name VARCHAR(255),
		Approvers2ID BIGINT,
		Approvers3Category VARCHAR(255),
		Approvers3Name VARCHAR(255),
		Approvers3ID BIGINT,
		Approvers4Category VARCHAR(255),
		Approvers4Name VARCHAR(255),
		Approvers4ID BIGINT,
		Approvers5Category VARCHAR(255),
		Approvers5Name VARCHAR(255),
		Approvers5ID BIGINT,
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceConditions', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceConditions (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		PerformanceTemplatesID BIGINT,
		AgeMin INT,
		AgeMax INT,
		ProfessionalBackground VARCHAR(255),
		EducationalBackground VARCHAR(255),
		CareerYearMin DECIMAL(3, 2),
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceCompetencyDetails', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceCompetencyDetails (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		PerformanceCompetenciesID BIGINT,
		Description VARCHAR(255),
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceTrainings', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceTrainings (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		PerformanceConditionsID BIGINT,
		Name VARCHAR(255),
		DateIn DATETIME DEFAULT CURRENT_TIMESTAMP,
		DateUp DATETIME,
		UserIn BIGINT,
		UserUp BIGINT,
		IsDeleted BIT DEFAULT 0
	)
END;

IF OBJECT_ID('dbo.PerformanceOptionWeights', 'U') IS NULL
BEGIN
    CREATE TABLE PerformanceOptionWeights (
		ID BIGINT IDENTITY(1,1) PRIMARY KEY,
		[Option] INT,
		Weight INT
	)
END;

COMMIT;