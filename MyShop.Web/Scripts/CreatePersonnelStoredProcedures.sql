-- Stored procedures ماژول پرسونل (CRUD)
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- List
IF OBJECT_ID('koch.spPersonnel_List', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_List;
GO
CREATE PROCEDURE koch.spPersonnel_List
    @StoreId INT = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, FullName, NationalCode, Phone, Email, Position, Department,
           ResumeText, ResumeFileUrl, HireDate, LeaveDate, BirthDate, Address, Specifications,
           DefaultWorkStartTime, DefaultWorkEndTime, ImageUrl, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Personnel
    WHERE (@StoreId IS NULL OR StoreId = @StoreId)
      AND (@IsActive IS NULL OR IsActive = @IsActive)
    ORDER BY SortOrder, FullName;
END
GO

-- GetById
IF OBJECT_ID('koch.spPersonnel_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_GetById;
GO
CREATE PROCEDURE koch.spPersonnel_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, FullName, NationalCode, Phone, Email, Position, Department,
           ResumeText, ResumeFileUrl, HireDate, LeaveDate, BirthDate, Address, Specifications,
           DefaultWorkStartTime, DefaultWorkEndTime, ImageUrl, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Personnel WHERE Id = @Id;
END
GO

-- Insert
IF OBJECT_ID('koch.spPersonnel_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_Insert;
GO
CREATE PROCEDURE koch.spPersonnel_Insert
    @StoreId INT = NULL,
    @FullName NVARCHAR(128),
    @NationalCode NVARCHAR(10) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @Email NVARCHAR(256) = NULL,
    @Position NVARCHAR(128) = NULL,
    @Department NVARCHAR(128) = NULL,
    @ResumeText NVARCHAR(MAX) = NULL,
    @ResumeFileUrl NVARCHAR(1024) = NULL,
    @HireDate DATE = NULL,
    @LeaveDate DATE = NULL,
    @BirthDate DATE = NULL,
    @Address NVARCHAR(512) = NULL,
    @Specifications NVARCHAR(MAX) = NULL,
    @DefaultWorkStartTime TIME(0) = NULL,
    @DefaultWorkEndTime TIME(0) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.Personnel (StoreId, FullName, NationalCode, Phone, Email, Position, Department,
        ResumeText, ResumeFileUrl, HireDate, LeaveDate, BirthDate, Address, Specifications,
        DefaultWorkStartTime, DefaultWorkEndTime, ImageUrl, IsActive, SortOrder, CreatedAt)
    VALUES (@StoreId, @FullName, @NationalCode, @Phone, @Email, @Position, @Department,
        @ResumeText, @ResumeFileUrl, @HireDate, @LeaveDate, @BirthDate, @Address, @Specifications,
        @DefaultWorkStartTime, @DefaultWorkEndTime, @ImageUrl, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

-- Update
IF OBJECT_ID('koch.spPersonnel_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_Update;
GO
CREATE PROCEDURE koch.spPersonnel_Update
    @Id INT,
    @StoreId INT = NULL,
    @FullName NVARCHAR(128),
    @NationalCode NVARCHAR(10) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @Email NVARCHAR(256) = NULL,
    @Position NVARCHAR(128) = NULL,
    @Department NVARCHAR(128) = NULL,
    @ResumeText NVARCHAR(MAX) = NULL,
    @ResumeFileUrl NVARCHAR(1024) = NULL,
    @HireDate DATE = NULL,
    @LeaveDate DATE = NULL,
    @BirthDate DATE = NULL,
    @Address NVARCHAR(512) = NULL,
    @Specifications NVARCHAR(MAX) = NULL,
    @DefaultWorkStartTime TIME(0) = NULL,
    @DefaultWorkEndTime TIME(0) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.Personnel SET
        StoreId = @StoreId, FullName = @FullName, NationalCode = @NationalCode, Phone = @Phone,
        Email = @Email, Position = @Position, Department = @Department, ResumeText = @ResumeText,
        ResumeFileUrl = @ResumeFileUrl, HireDate = @HireDate, LeaveDate = @LeaveDate, BirthDate = @BirthDate,
        Address = @Address, Specifications = @Specifications, DefaultWorkStartTime = @DefaultWorkStartTime,
        DefaultWorkEndTime = @DefaultWorkEndTime, ImageUrl = @ImageUrl, IsActive = @IsActive, SortOrder = @SortOrder,
        UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- Delete
IF OBJECT_ID('koch.spPersonnel_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_Delete;
GO
CREATE PROCEDURE koch.spPersonnel_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.Personnel WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
