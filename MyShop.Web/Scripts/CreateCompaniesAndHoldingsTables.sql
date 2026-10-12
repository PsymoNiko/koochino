-- ماژول شرکت‌ها و هلدینگ: جداول Holdings و Companies + اضافه کردن CompanyId به جداول موجود
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- جدول هلدینگ‌ها
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Holdings')
BEGIN
    CREATE TABLE [koch].[Holdings] (
        [Id]          INT IDENTITY(1,1) NOT NULL,
        [Code]        NVARCHAR(32) NULL,
        [Name]        NVARCHAR(128) NOT NULL,
        [Description] NVARCHAR(512) NULL,
        [IsActive]    BIT NOT NULL DEFAULT 1,
        [SortOrder]   INT NOT NULL DEFAULT 0,
        [CreatedAt]   DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]   DATETIME2(2) NULL,
        CONSTRAINT [PK_Holdings] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

-- جدول شرکت‌ها
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Companies')
BEGIN
    CREATE TABLE [koch].[Companies] (
        [Id]          INT IDENTITY(1,1) NOT NULL,
        [HoldingId]    INT NOT NULL,
        [Code]        NVARCHAR(32) NULL,
        [Name]        NVARCHAR(128) NOT NULL,
        [Address]     NVARCHAR(512) NULL,
        [Phone]       NVARCHAR(32) NULL,
        [Description] NVARCHAR(512) NULL,
        [IsActive]    BIT NOT NULL DEFAULT 1,
        [SortOrder]   INT NOT NULL DEFAULT 0,
        [CreatedAt]   DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]   DATETIME2(2) NULL,
        CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Companies_Holding] FOREIGN KEY ([HoldingId]) REFERENCES [koch].[Holdings]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_Companies_HoldingId] ON [koch].[Companies] ([HoldingId]);
END
GO

-- اضافه کردن CompanyId به Store (جدول Stores در schema dbo یا koch - بر اساس پروژه)
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'dbo' AND t.name = 'Stores')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Stores') AND name = 'CompanyId')
        ALTER TABLE [dbo].[Stores] ADD [CompanyId] INT NULL;
END
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Stores')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Stores') AND name = 'CompanyId')
        ALTER TABLE [koch].[Stores] ADD [CompanyId] INT NULL;
END
GO

-- اضافه کردن CompanyId به Warehouses
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Warehouses')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Warehouses') AND name = 'CompanyId')
        ALTER TABLE [koch].[Warehouses] ADD [CompanyId] INT NULL;
END
GO

-- اضافه کردن CompanyId به Personnel
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Personnel')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Personnel') AND name = 'CompanyId')
        ALTER TABLE [koch].[Personnel] ADD [CompanyId] INT NULL;
END
GO

-- اضافه کردن CompanyId به Assets، FinanceAccounts، CrmContacts، Contracts، Projects، Products، TrainingCourses (در صورت وجود جدول)
DECLARE @sql NVARCHAR(500);
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Assets')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Assets') AND name = 'CompanyId')
        ALTER TABLE [koch].[Assets] ADD [CompanyId] INT NULL;
END
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'FinanceAccounts')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.FinanceAccounts') AND name = 'CompanyId')
        ALTER TABLE [koch].[FinanceAccounts] ADD [CompanyId] INT NULL;
END
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'CrmContacts')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.CrmContacts') AND name = 'CompanyId')
        ALTER TABLE [koch].[CrmContacts] ADD [CompanyId] INT NULL;
END
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Contracts')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Contracts') AND name = 'CompanyId')
        ALTER TABLE [koch].[Contracts] ADD [CompanyId] INT NULL;
END
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Projects')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Projects') AND name = 'CompanyId')
        ALTER TABLE [koch].[Projects] ADD [CompanyId] INT NULL;
END
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Products')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Products') AND name = 'CompanyId')
        ALTER TABLE [koch].[Products] ADD [CompanyId] INT NULL;
END
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'TrainingCourses')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.TrainingCourses') AND name = 'CompanyId')
        ALTER TABLE [koch].[TrainingCourses] ADD [CompanyId] INT NULL;
END
GO
