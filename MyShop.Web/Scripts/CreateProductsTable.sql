-- جدول محصولات (ماژول محصولات) — هر آنچه یک محصول نیاز دارد
-- اسکریپت را روی دیتابیس مورد نظر اجرا کنید (مثلاً [koch] یا [dbo]).

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch')
    EXEC('CREATE SCHEMA koch');
GO

IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Products')
BEGIN
    CREATE TABLE [koch].[Products] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [StoreId]           INT NULL,
        [CategoryId]        INT NULL,
        [Name]              NVARCHAR(256) NOT NULL,
        [Slug]              NVARCHAR(300) NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [ShortDescription]  NVARCHAR(512) NULL,
        [Price]             DECIMAL(18,2) NULL,
        [CompareAtPrice]    DECIMAL(18,2) NULL,
        [Cost]              DECIMAL(18,2) NULL,
        [Sku]               NVARCHAR(64) NULL,
        [Barcode]           NVARCHAR(64) NULL,
        [Quantity]          INT NOT NULL DEFAULT 0,
        [IsActive]          BIT NOT NULL DEFAULT 1,
        [IsFeatured]        BIT NOT NULL DEFAULT 0,
        [ImageUrl]          NVARCHAR(1024) NULL,
        [SortOrder]         INT NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]         DATETIME2(2) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Products_StoreId] ON [koch].[Products] ([StoreId]);
    CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [koch].[Products] ([CategoryId]);
    CREATE NONCLUSTERED INDEX [IX_Products_IsActive_SortOrder] ON [koch].[Products] ([IsActive], [SortOrder]);
END
GO

-- جدول دسته‌بندی محصول (اختیاری؛ در صورت نیاز)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'ProductCategories')
BEGIN
    CREATE TABLE [koch].[ProductCategories] (
        [Id]          INT IDENTITY(1,1) NOT NULL,
        [ParentId]    INT NULL,
        [Name]        NVARCHAR(128) NOT NULL,
        [Slug]        NVARCHAR(150) NULL,
        [SortOrder]   INT NOT NULL DEFAULT 0,
        [IsActive]    BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProductCategories_Parent] FOREIGN KEY ([ParentId]) REFERENCES [koch].[ProductCategories]([Id])
    );
END
GO
