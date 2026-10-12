-- SPهای دوره آموزشی - CRUD
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF OBJECT_ID('koch.spTrainingCourses_List', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_List;
GO
CREATE PROCEDURE koch.spTrainingCourses_List
    @StoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, Code, Title, Description, DurationHours, StartDate, EndDate, MaxParticipants, Location, InstructorName, ImageUrl, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.TrainingCourses
    WHERE (@StoreId IS NULL OR StoreId = @StoreId)
    ORDER BY SortOrder, Title;
END
GO

IF OBJECT_ID('koch.spTrainingCourses_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_GetById;
GO
CREATE PROCEDURE koch.spTrainingCourses_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, Code, Title, Description, DurationHours, StartDate, EndDate, MaxParticipants, Location, InstructorName, ImageUrl, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.TrainingCourses
    WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spTrainingCourses_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_Insert;
GO
CREATE PROCEDURE koch.spTrainingCourses_Insert
    @StoreId INT = NULL,
    @Code NVARCHAR(32) = NULL,
    @Title NVARCHAR(256),
    @Description NVARCHAR(MAX) = NULL,
    @DurationHours DECIMAL(6,2) = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @MaxParticipants INT = NULL,
    @Location NVARCHAR(256) = NULL,
    @InstructorName NVARCHAR(128) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.TrainingCourses (StoreId, Code, Title, Description, DurationHours, StartDate, EndDate, MaxParticipants, Location, InstructorName, ImageUrl, IsActive, SortOrder, CreatedAt)
    VALUES (@StoreId, @Code, @Title, @Description, @DurationHours, @StartDate, @EndDate, @MaxParticipants, @Location, @InstructorName, @ImageUrl, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spTrainingCourses_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_Update;
GO
CREATE PROCEDURE koch.spTrainingCourses_Update
    @Id INT,
    @StoreId INT = NULL,
    @Code NVARCHAR(32) = NULL,
    @Title NVARCHAR(256),
    @Description NVARCHAR(MAX) = NULL,
    @DurationHours DECIMAL(6,2) = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @MaxParticipants INT = NULL,
    @Location NVARCHAR(256) = NULL,
    @InstructorName NVARCHAR(128) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.TrainingCourses
    SET StoreId = @StoreId, Code = @Code, Title = @Title, Description = @Description, DurationHours = @DurationHours,
        StartDate = @StartDate, EndDate = @EndDate, MaxParticipants = @MaxParticipants, Location = @Location,
        InstructorName = @InstructorName, ImageUrl = @ImageUrl, IsActive = @IsActive, SortOrder = @SortOrder, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spTrainingCourses_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_Delete;
GO
CREATE PROCEDURE koch.spTrainingCourses_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.TrainingCourses WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
