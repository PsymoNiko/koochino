-- SPهای دارایی + لیست دسته - CRUD
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF OBJECT_ID('koch.spAssetCategories_List', 'P') IS NOT NULL DROP PROCEDURE koch.spAssetCategories_List;
GO
CREATE PROCEDURE koch.spAssetCategories_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name FROM koch.AssetCategories WHERE IsActive = 1 ORDER BY SortOrder, Name;
END
GO

IF OBJECT_ID('koch.spAssets_List', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_List;
GO
CREATE PROCEDURE koch.spAssets_List
    @StoreId INT = NULL,
    @CategoryId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, CategoryId, Code, Name, Description, PurchaseDate, PurchasePrice, CurrentValue, Location, AssignedTo, PersonnelId, Status, WarrantyEndDate, DepreciationRate, ImageUrl, SerialNo, Notes, IsActive, CreatedAt, UpdatedAt
    FROM koch.Assets
    WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@CategoryId IS NULL OR CategoryId = @CategoryId)
    ORDER BY Name;
END
GO

IF OBJECT_ID('koch.spAssets_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_GetById;
GO
CREATE PROCEDURE koch.spAssets_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, CategoryId, Code, Name, Description, PurchaseDate, PurchasePrice, CurrentValue, Location, AssignedTo, PersonnelId, Status, WarrantyEndDate, DepreciationRate, ImageUrl, SerialNo, Notes, IsActive, CreatedAt, UpdatedAt
    FROM koch.Assets
    WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spAssets_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_Insert;
GO
CREATE PROCEDURE koch.spAssets_Insert
    @StoreId INT = NULL,
    @CategoryId INT = NULL,
    @Code NVARCHAR(64) = NULL,
    @Name NVARCHAR(256),
    @Description NVARCHAR(MAX) = NULL,
    @PurchaseDate DATE = NULL,
    @PurchasePrice DECIMAL(18,2) = NULL,
    @CurrentValue DECIMAL(18,2) = NULL,
    @Location NVARCHAR(256) = NULL,
    @AssignedTo INT = NULL,
    @PersonnelId INT = NULL,
    @Status NVARCHAR(32) = NULL,
    @WarrantyEndDate DATE = NULL,
    @DepreciationRate DECIMAL(5,2) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL,
    @SerialNo NVARCHAR(128) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
    @IsActive BIT = 1,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.Assets (StoreId, CategoryId, Code, Name, Description, PurchaseDate, PurchasePrice, CurrentValue, Location, AssignedTo, PersonnelId, Status, WarrantyEndDate, DepreciationRate, ImageUrl, SerialNo, Notes, IsActive, CreatedAt)
    VALUES (@StoreId, @CategoryId, @Code, @Name, @Description, @PurchaseDate, @PurchasePrice, @CurrentValue, @Location, @AssignedTo, @PersonnelId, @Status, @WarrantyEndDate, @DepreciationRate, @ImageUrl, @SerialNo, @Notes, @IsActive, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spAssets_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_Update;
GO
CREATE PROCEDURE koch.spAssets_Update
    @Id INT,
    @StoreId INT = NULL,
    @CategoryId INT = NULL,
    @Code NVARCHAR(64) = NULL,
    @Name NVARCHAR(256),
    @Description NVARCHAR(MAX) = NULL,
    @PurchaseDate DATE = NULL,
    @PurchasePrice DECIMAL(18,2) = NULL,
    @CurrentValue DECIMAL(18,2) = NULL,
    @Location NVARCHAR(256) = NULL,
    @AssignedTo INT = NULL,
    @PersonnelId INT = NULL,
    @Status NVARCHAR(32) = NULL,
    @WarrantyEndDate DATE = NULL,
    @DepreciationRate DECIMAL(5,2) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL,
    @SerialNo NVARCHAR(128) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
    @IsActive BIT = 1,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.Assets
    SET StoreId = @StoreId, CategoryId = @CategoryId, Code = @Code, Name = @Name, Description = @Description,
        PurchaseDate = @PurchaseDate, PurchasePrice = @PurchasePrice, CurrentValue = @CurrentValue, Location = @Location,
        AssignedTo = @AssignedTo, PersonnelId = @PersonnelId, Status = @Status, WarrantyEndDate = @WarrantyEndDate,
        DepreciationRate = @DepreciationRate, ImageUrl = @ImageUrl, SerialNo = @SerialNo, Notes = @Notes, IsActive = @IsActive, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spAssets_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_Delete;
GO
CREATE PROCEDURE koch.spAssets_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.Assets WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
