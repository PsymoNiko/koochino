-- اضافه کردن ستون‌های گم‌شده به koch.Products (در صورت وجود نداشتن)
-- این اسکریپت را قبل از CreateProductStoredProcedures.sql اجرا کنید اگر جدول از قبل با ستون‌های کمتر ساخته شده است.

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Products')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'CategoryId')
        ALTER TABLE koch.Products ADD CategoryId INT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'Slug')
        ALTER TABLE koch.Products ADD Slug NVARCHAR(300) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'ShortDescription')
        ALTER TABLE koch.Products ADD ShortDescription NVARCHAR(512) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'CompareAtPrice')
        ALTER TABLE koch.Products ADD CompareAtPrice DECIMAL(18,2) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'Cost')
        ALTER TABLE koch.Products ADD Cost DECIMAL(18,2) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'Sku')
        ALTER TABLE koch.Products ADD Sku NVARCHAR(64) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'Barcode')
        ALTER TABLE koch.Products ADD Barcode NVARCHAR(64) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'Quantity')
        ALTER TABLE koch.Products ADD Quantity INT NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'IsFeatured')
        ALTER TABLE koch.Products ADD IsFeatured BIT NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'SortOrder')
        ALTER TABLE koch.Products ADD SortOrder INT NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'UpdatedAt')
        ALTER TABLE koch.Products ADD UpdatedAt DATETIME2(2) NULL;
END
GO
