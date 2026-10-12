-- فاز ۳: اضافه کردن CompanyId به SPهای انبار و پرسنل (اجرا بعد از CreateCompaniesAndHoldingsTables.sql)
USE [koch]
GO

-- ========== انبار (Warehouses) ==========
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Warehouses') AND name = 'CompanyId')
    ALTER TABLE [koch].[Warehouses] ADD [CompanyId] INT NULL;
GO

IF OBJECT_ID('koch.spWarehouses_List', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_List;
GO
CREATE PROCEDURE koch.spWarehouses_List
    @StoreId INT = NULL,
    @CompanyId INT = NULL,
    @IsActive BIT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, Code, Name, Address, Phone, ManagerName, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Warehouses
    WHERE (@StoreId IS NULL OR StoreId = @StoreId)
      AND (@CompanyId IS NULL OR CompanyId = @CompanyId)
      AND (@IsActive IS NULL OR IsActive = @IsActive)
    ORDER BY SortOrder, Name;
END
GO

IF OBJECT_ID('koch.spWarehouses_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_GetById;
GO
CREATE PROCEDURE koch.spWarehouses_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, Code, Name, Address, Phone, ManagerName, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Warehouses WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spWarehouses_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_Insert;
GO
CREATE PROCEDURE koch.spWarehouses_Insert
    @StoreId INT = NULL, @CompanyId INT = NULL, @Code NVARCHAR(32) = NULL, @Name NVARCHAR(128),
    @Address NVARCHAR(512) = NULL, @Phone NVARCHAR(20) = NULL, @ManagerName NVARCHAR(128) = NULL,
    @IsActive BIT = 1, @SortOrder INT = 0, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.Warehouses (StoreId, CompanyId, Code, Name, Address, Phone, ManagerName, IsActive, SortOrder, CreatedAt)
    VALUES (@StoreId, @CompanyId, @Code, @Name, @Address, @Phone, @ManagerName, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spWarehouses_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_Update;
GO
CREATE PROCEDURE koch.spWarehouses_Update
    @Id INT, @StoreId INT = NULL, @CompanyId INT = NULL, @Code NVARCHAR(32) = NULL, @Name NVARCHAR(128),
    @Address NVARCHAR(512) = NULL, @Phone NVARCHAR(20) = NULL, @ManagerName NVARCHAR(128) = NULL,
    @IsActive BIT = 1, @SortOrder INT = 0, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.Warehouses SET StoreId=@StoreId, CompanyId=@CompanyId, Code=@Code, Name=@Name, Address=@Address, Phone=@Phone, ManagerName=@ManagerName, IsActive=@IsActive, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- ========== پرسنل (Personnel) ==========
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Personnel') AND name = 'CompanyId')
    ALTER TABLE [koch].[Personnel] ADD [CompanyId] INT NULL;
GO

IF OBJECT_ID('koch.spPersonnel_List', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_List;
GO
CREATE PROCEDURE koch.spPersonnel_List
    @StoreId INT = NULL,
    @CompanyId INT = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, FullName, NationalCode, Phone, Email, Position, Department,
           ResumeText, ResumeFileUrl, HireDate, LeaveDate, BirthDate, Address, Specifications,
           DefaultWorkStartTime, DefaultWorkEndTime, ImageUrl, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Personnel
    WHERE (@StoreId IS NULL OR StoreId = @StoreId)
      AND (@CompanyId IS NULL OR CompanyId = @CompanyId)
      AND (@IsActive IS NULL OR IsActive = @IsActive)
    ORDER BY SortOrder, FullName;
END
GO

IF OBJECT_ID('koch.spPersonnel_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_GetById;
GO
CREATE PROCEDURE koch.spPersonnel_GetById @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, FullName, NationalCode, Phone, Email, Position, Department,
           ResumeText, ResumeFileUrl, HireDate, LeaveDate, BirthDate, Address, Specifications,
           DefaultWorkStartTime, DefaultWorkEndTime, ImageUrl, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Personnel WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spPersonnel_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_Insert;
GO
CREATE PROCEDURE koch.spPersonnel_Insert
    @StoreId INT = NULL, @CompanyId INT = NULL,
    @FullName NVARCHAR(128), @NationalCode NVARCHAR(10) = NULL, @Phone NVARCHAR(20) = NULL, @Email NVARCHAR(256) = NULL,
    @Position NVARCHAR(128) = NULL, @Department NVARCHAR(128) = NULL, @ResumeText NVARCHAR(MAX) = NULL, @ResumeFileUrl NVARCHAR(1024) = NULL,
    @HireDate DATE = NULL, @LeaveDate DATE = NULL, @BirthDate DATE = NULL, @Address NVARCHAR(512) = NULL, @Specifications NVARCHAR(MAX) = NULL,
    @DefaultWorkStartTime TIME(0) = NULL, @DefaultWorkEndTime TIME(0) = NULL, @ImageUrl NVARCHAR(1024) = NULL,
    @IsActive BIT = 1, @SortOrder INT = 0, @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.Personnel (StoreId, CompanyId, FullName, NationalCode, Phone, Email, Position, Department,
        ResumeText, ResumeFileUrl, HireDate, LeaveDate, BirthDate, Address, Specifications,
        DefaultWorkStartTime, DefaultWorkEndTime, ImageUrl, IsActive, SortOrder, CreatedAt)
    VALUES (@StoreId, @CompanyId, @FullName, @NationalCode, @Phone, @Email, @Position, @Department,
        @ResumeText, @ResumeFileUrl, @HireDate, @LeaveDate, @BirthDate, @Address, @Specifications,
        @DefaultWorkStartTime, @DefaultWorkEndTime, @ImageUrl, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spPersonnel_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spPersonnel_Update;
GO
CREATE PROCEDURE koch.spPersonnel_Update
    @Id INT, @StoreId INT = NULL, @CompanyId INT = NULL,
    @FullName NVARCHAR(128), @NationalCode NVARCHAR(10) = NULL, @Phone NVARCHAR(20) = NULL, @Email NVARCHAR(256) = NULL,
    @Position NVARCHAR(128) = NULL, @Department NVARCHAR(128) = NULL, @ResumeText NVARCHAR(MAX) = NULL, @ResumeFileUrl NVARCHAR(1024) = NULL,
    @HireDate DATE = NULL, @LeaveDate DATE = NULL, @BirthDate DATE = NULL, @Address NVARCHAR(512) = NULL, @Specifications NVARCHAR(MAX) = NULL,
    @DefaultWorkStartTime TIME(0) = NULL, @DefaultWorkEndTime TIME(0) = NULL, @ImageUrl NVARCHAR(1024) = NULL,
    @IsActive BIT = 1, @SortOrder INT = 0, @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.Personnel SET
        StoreId = @StoreId, CompanyId = @CompanyId, FullName = @FullName, NationalCode = @NationalCode, Phone = @Phone,
        Email = @Email, Position = @Position, Department = @Department, ResumeText = @ResumeText, ResumeFileUrl = @ResumeFileUrl,
        HireDate = @HireDate, LeaveDate = @LeaveDate, BirthDate = @BirthDate, Address = @Address, Specifications = @Specifications,
        DefaultWorkStartTime = @DefaultWorkStartTime, DefaultWorkEndTime = @DefaultWorkEndTime, ImageUrl = @ImageUrl,
        IsActive = @IsActive, SortOrder = @SortOrder, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

PRINT 'CompanyId به SPهای انبار و پرسنل اضافه شد.';
GO
