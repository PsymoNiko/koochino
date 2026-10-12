-- SPهای حساب مالی - CRUD
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF OBJECT_ID('koch.spFinanceAccounts_List', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_List;
GO
CREATE PROCEDURE koch.spFinanceAccounts_List
    @StoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, ParentId, Code, Name, Type, Balance, Currency, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.FinanceAccounts
    WHERE (@StoreId IS NULL OR StoreId = @StoreId)
    ORDER BY SortOrder, Code;
END
GO

IF OBJECT_ID('koch.spFinanceAccounts_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_GetById;
GO
CREATE PROCEDURE koch.spFinanceAccounts_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, ParentId, Code, Name, Type, Balance, Currency, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.FinanceAccounts
    WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spFinanceAccounts_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_Insert;
GO
CREATE PROCEDURE koch.spFinanceAccounts_Insert
    @StoreId INT = NULL,
    @ParentId INT = NULL,
    @Code NVARCHAR(32),
    @Name NVARCHAR(256),
    @Type NVARCHAR(32) = NULL,
    @Balance DECIMAL(18,2) = 0,
    @Currency NVARCHAR(8) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.FinanceAccounts (StoreId, ParentId, Code, Name, Type, Balance, Currency, IsActive, SortOrder, CreatedAt)
    VALUES (@StoreId, @ParentId, @Code, @Name, @Type, @Balance, @Currency, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spFinanceAccounts_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_Update;
GO
CREATE PROCEDURE koch.spFinanceAccounts_Update
    @Id INT,
    @StoreId INT = NULL,
    @ParentId INT = NULL,
    @Code NVARCHAR(32),
    @Name NVARCHAR(256),
    @Type NVARCHAR(32) = NULL,
    @Balance DECIMAL(18,2) = 0,
    @Currency NVARCHAR(8) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.FinanceAccounts
    SET StoreId = @StoreId, ParentId = @ParentId, Code = @Code, Name = @Name, Type = @Type, Balance = @Balance,
        Currency = @Currency, IsActive = @IsActive, SortOrder = @SortOrder, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spFinanceAccounts_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_Delete;
GO
CREATE PROCEDURE koch.spFinanceAccounts_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.FinanceAccounts WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
