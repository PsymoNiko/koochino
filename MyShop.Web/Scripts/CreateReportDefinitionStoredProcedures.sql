-- SPهای تعریف گزارش - CRUD
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF OBJECT_ID('koch.spReportDefinitions_List', 'P') IS NOT NULL DROP PROCEDURE koch.spReportDefinitions_List;
GO
CREATE PROCEDURE koch.spReportDefinitions_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code, Name, Description, ModuleKey, QueryOrConfig, OutputType, ParametersJson, IsActive, SortOrder, CreatedBy, CreatedAt, UpdatedAt
    FROM koch.ReportDefinitions
    ORDER BY SortOrder, Name;
END
GO

IF OBJECT_ID('koch.spReportDefinitions_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spReportDefinitions_GetById;
GO
CREATE PROCEDURE koch.spReportDefinitions_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code, Name, Description, ModuleKey, QueryOrConfig, OutputType, ParametersJson, IsActive, SortOrder, CreatedBy, CreatedAt, UpdatedAt
    FROM koch.ReportDefinitions
    WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spReportDefinitions_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spReportDefinitions_Insert;
GO
CREATE PROCEDURE koch.spReportDefinitions_Insert
    @Code NVARCHAR(64) = NULL,
    @Name NVARCHAR(256),
    @Description NVARCHAR(512) = NULL,
    @ModuleKey NVARCHAR(64) = NULL,
    @QueryOrConfig NVARCHAR(MAX) = NULL,
    @OutputType NVARCHAR(32) = NULL,
    @ParametersJson NVARCHAR(MAX) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.ReportDefinitions (Code, Name, Description, ModuleKey, QueryOrConfig, OutputType, ParametersJson, IsActive, SortOrder, CreatedAt)
    VALUES (@Code, @Name, @Description, @ModuleKey, @QueryOrConfig, @OutputType, @ParametersJson, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spReportDefinitions_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spReportDefinitions_Update;
GO
CREATE PROCEDURE koch.spReportDefinitions_Update
    @Id INT,
    @Code NVARCHAR(64) = NULL,
    @Name NVARCHAR(256),
    @Description NVARCHAR(512) = NULL,
    @ModuleKey NVARCHAR(64) = NULL,
    @QueryOrConfig NVARCHAR(MAX) = NULL,
    @OutputType NVARCHAR(32) = NULL,
    @ParametersJson NVARCHAR(MAX) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.ReportDefinitions
    SET Code = @Code, Name = @Name, Description = @Description, ModuleKey = @ModuleKey, QueryOrConfig = @QueryOrConfig,
        OutputType = @OutputType, ParametersJson = @ParametersJson, IsActive = @IsActive, SortOrder = @SortOrder, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spReportDefinitions_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spReportDefinitions_Delete;
GO
CREATE PROCEDURE koch.spReportDefinitions_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.ReportDefinitions WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
