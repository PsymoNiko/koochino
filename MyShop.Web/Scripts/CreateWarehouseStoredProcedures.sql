-- SPهای انبار (Warehouses) - CRUD
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF OBJECT_ID('koch.spWarehouses_List', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_List;
GO
CREATE PROCEDURE koch.spWarehouses_List @StoreId INT = NULL, @IsActive BIT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, StoreId, Code, Name, Address, Phone, ManagerName, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Warehouses WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@IsActive IS NULL OR IsActive = @IsActive) ORDER BY SortOrder, Name;
END
GO

IF OBJECT_ID('koch.spWarehouses_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_GetById;
GO
CREATE PROCEDURE koch.spWarehouses_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, StoreId, Code, Name, Address, Phone, ManagerName, IsActive, SortOrder, CreatedAt, UpdatedAt FROM koch.Warehouses WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spWarehouses_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_Insert;
GO
CREATE PROCEDURE koch.spWarehouses_Insert
    @StoreId INT = NULL, @Code NVARCHAR(32) = NULL, @Name NVARCHAR(128), @Address NVARCHAR(512) = NULL,
    @Phone NVARCHAR(20) = NULL, @ManagerName NVARCHAR(128) = NULL, @IsActive BIT = 1, @SortOrder INT = 0, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.Warehouses (StoreId, Code, Name, Address, Phone, ManagerName, IsActive, SortOrder, CreatedAt)
    VALUES (@StoreId, @Code, @Name, @Address, @Phone, @ManagerName, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spWarehouses_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_Update;
GO
CREATE PROCEDURE koch.spWarehouses_Update
    @Id INT, @StoreId INT = NULL, @Code NVARCHAR(32) = NULL, @Name NVARCHAR(128), @Address NVARCHAR(512) = NULL,
    @Phone NVARCHAR(20) = NULL, @ManagerName NVARCHAR(128) = NULL, @IsActive BIT = 1, @SortOrder INT = 0, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.Warehouses SET StoreId=@StoreId, Code=@Code, Name=@Name, Address=@Address, Phone=@Phone, ManagerName=@ManagerName, IsActive=@IsActive, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spWarehouses_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spWarehouses_Delete;
GO
CREATE PROCEDURE koch.spWarehouses_Delete @Id INT, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON; DELETE FROM koch.Warehouses WHERE Id = @Id; SET @Result = @@ROWCOUNT; END
GO
