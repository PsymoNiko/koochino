-- SPهای محصول - CRUD
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF OBJECT_ID('koch.spProducts_List', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_List;
GO
CREATE PROCEDURE koch.spProducts_List
    @StoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, CategoryId, Name, Slug, Description, ShortDescription, Price, CompareAtPrice, Cost, Sku, Barcode, Quantity, IsActive, IsFeatured, ImageUrl, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Products
    WHERE (@StoreId IS NULL OR StoreId = @StoreId)
    ORDER BY SortOrder, Name;
END
GO

IF OBJECT_ID('koch.spProducts_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_GetById;
GO
CREATE PROCEDURE koch.spProducts_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, CategoryId, Name, Slug, Description, ShortDescription, Price, CompareAtPrice, Cost, Sku, Barcode, Quantity, IsActive, IsFeatured, ImageUrl, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Products
    WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spProducts_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_Insert;
GO
CREATE PROCEDURE koch.spProducts_Insert
    @StoreId INT = NULL,
    @CategoryId INT = NULL,
    @Name NVARCHAR(256),
    @Slug NVARCHAR(300) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @ShortDescription NVARCHAR(512) = NULL,
    @Price DECIMAL(18,2) = NULL,
    @CompareAtPrice DECIMAL(18,2) = NULL,
    @Cost DECIMAL(18,2) = NULL,
    @Sku NVARCHAR(64) = NULL,
    @Barcode NVARCHAR(64) = NULL,
    @Quantity INT = 0,
    @IsActive BIT = 1,
    @IsFeatured BIT = 0,
    @ImageUrl NVARCHAR(1024) = NULL,
    @SortOrder INT = 0,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.Products (StoreId, CategoryId, Name, Slug, Description, ShortDescription, Price, CompareAtPrice, Cost, Sku, Barcode, Quantity, IsActive, IsFeatured, ImageUrl, SortOrder, CreatedAt)
    VALUES (@StoreId, @CategoryId, @Name, @Slug, @Description, @ShortDescription, @Price, @CompareAtPrice, @Cost, @Sku, @Barcode, @Quantity, @IsActive, @IsFeatured, @ImageUrl, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spProducts_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_Update;
GO
CREATE PROCEDURE koch.spProducts_Update
    @Id INT,
    @StoreId INT = NULL,
    @CategoryId INT = NULL,
    @Name NVARCHAR(256),
    @Slug NVARCHAR(300) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @ShortDescription NVARCHAR(512) = NULL,
    @Price DECIMAL(18,2) = NULL,
    @CompareAtPrice DECIMAL(18,2) = NULL,
    @Cost DECIMAL(18,2) = NULL,
    @Sku NVARCHAR(64) = NULL,
    @Barcode NVARCHAR(64) = NULL,
    @Quantity INT = 0,
    @IsActive BIT = 1,
    @IsFeatured BIT = 0,
    @ImageUrl NVARCHAR(1024) = NULL,
    @SortOrder INT = 0,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.Products
    SET StoreId = @StoreId, CategoryId = @CategoryId, Name = @Name, Slug = @Slug, Description = @Description, ShortDescription = @ShortDescription,
        Price = @Price, CompareAtPrice = @CompareAtPrice, Cost = @Cost, Sku = @Sku, Barcode = @Barcode, Quantity = @Quantity,
        IsActive = @IsActive, IsFeatured = @IsFeatured, ImageUrl = @ImageUrl, SortOrder = @SortOrder, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spProducts_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_Delete;
GO
CREATE PROCEDURE koch.spProducts_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.Products WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
