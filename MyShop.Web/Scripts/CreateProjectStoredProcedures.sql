-- SPهای پروژه - CRUD
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF OBJECT_ID('koch.spProjects_List', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_List;
GO
CREATE PROCEDURE koch.spProjects_List
    @StoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, Title, Description, Status, Priority, StartDate, EndDate, Progress, CreatedBy, CreatedAt, UpdatedAt
    FROM koch.Projects
    WHERE (@StoreId IS NULL OR StoreId = @StoreId)
    ORDER BY CreatedAt DESC;
END
GO

IF OBJECT_ID('koch.spProjects_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_GetById;
GO
CREATE PROCEDURE koch.spProjects_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, Title, Description, Status, Priority, StartDate, EndDate, Progress, CreatedBy, CreatedAt, UpdatedAt
    FROM koch.Projects
    WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spProjects_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_Insert;
GO
CREATE PROCEDURE koch.spProjects_Insert
    @StoreId INT = NULL,
    @Title NVARCHAR(256),
    @Description NVARCHAR(MAX) = NULL,
    @Status NVARCHAR(32) = N'Planning',
    @Priority NVARCHAR(32) = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @Progress TINYINT = 0,
    @CreatedBy INT = NULL,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.Projects (StoreId, Title, Description, Status, Priority, StartDate, EndDate, Progress, CreatedBy, CreatedAt)
    VALUES (@StoreId, @Title, @Description, @Status, @Priority, @StartDate, @EndDate, @Progress, @CreatedBy, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spProjects_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_Update;
GO
CREATE PROCEDURE koch.spProjects_Update
    @Id INT,
    @StoreId INT = NULL,
    @Title NVARCHAR(256),
    @Description NVARCHAR(MAX) = NULL,
    @Status NVARCHAR(32),
    @Priority NVARCHAR(32) = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @Progress TINYINT = 0,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.Projects
    SET StoreId = @StoreId, Title = @Title, Description = @Description, Status = @Status, Priority = @Priority,
        StartDate = @StartDate, EndDate = @EndDate, Progress = @Progress, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spProjects_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_Delete;
GO
CREATE PROCEDURE koch.spProjects_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.Projects WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
