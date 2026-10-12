-- ماژول دارایی‌ها و تجهیزات: دسته، دارایی، نگهداری، تاریخ انقضای گارانتی
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- دسته دارایی
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'AssetCategories')
BEGIN
    CREATE TABLE [koch].[AssetCategories] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ParentId]      INT NULL,
        [Name]          NVARCHAR(128) NOT NULL,
        [Code]          NVARCHAR(32) NULL,
        [Description]   NVARCHAR(512) NULL,
        [IsActive]      BIT NOT NULL DEFAULT 1,
        [SortOrder]     INT NOT NULL DEFAULT 0,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_AssetCategories] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AssetCategories_Parent] FOREIGN KEY ([ParentId]) REFERENCES [koch].[AssetCategories]([Id]) ON DELETE NO ACTION
    );
END
GO

-- دارایی‌ها
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Assets')
BEGIN
    CREATE TABLE [koch].[Assets] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [StoreId]           INT NULL,
        [CategoryId]        INT NULL,
        [Code]              NVARCHAR(64) NULL,
        [Name]              NVARCHAR(256) NOT NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [PurchaseDate]      DATE NULL,
        [PurchasePrice]     DECIMAL(18,2) NULL,
        [CurrentValue]      DECIMAL(18,2) NULL,
        [Location]          NVARCHAR(256) NULL,
        [AssignedTo]        INT NULL,
        [PersonnelId]       INT NULL,
        [Status]            NVARCHAR(32) NULL,
        [WarrantyEndDate]   DATE NULL,
        [DepreciationRate]  DECIMAL(5,2) NULL,
        [ImageUrl]          NVARCHAR(1024) NULL,
        [SerialNo]          NVARCHAR(128) NULL,
        [Notes]             NVARCHAR(MAX) NULL,
        [IsActive]          BIT NOT NULL DEFAULT 1,
        [CreatedAt]         DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]         DATETIME2(2) NULL,
        CONSTRAINT [PK_Assets] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Assets_Category] FOREIGN KEY ([CategoryId]) REFERENCES [koch].[AssetCategories]([Id]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_Assets_StoreId] ON [koch].[Assets] ([StoreId]);
    CREATE NONCLUSTERED INDEX [IX_Assets_CategoryId] ON [koch].[Assets] ([CategoryId]);
    CREATE NONCLUSTERED INDEX [IX_Assets_Status] ON [koch].[Assets] ([Status]);
END
GO

-- سوابق نگهداری/تعمیر
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'AssetMaintenances')
BEGIN
    CREATE TABLE [koch].[AssetMaintenances] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [AssetId]       INT NOT NULL,
        [MaintenanceDate] DATE NOT NULL,
        [Type]          NVARCHAR(64) NULL,
        [Cost]          DECIMAL(18,2) NULL,
        [Description]   NVARCHAR(MAX) NULL,
        [NextDueDate]   DATE NULL,
        [CreatedBy]     INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_AssetMaintenances] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AssetMaintenances_Asset] FOREIGN KEY ([AssetId]) REFERENCES [koch].[Assets]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_AssetMaintenances_AssetId] ON [koch].[AssetMaintenances] ([AssetId]);
END
GO
