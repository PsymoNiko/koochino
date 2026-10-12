-- ماژول انبار و موجودی: انبارها، موجودی، رسید/حواله، انتقال، تنظیم موجودی
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- انبارها
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Warehouses')
BEGIN
    CREATE TABLE [koch].[Warehouses] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [StoreId]       INT NULL,
        [Code]          NVARCHAR(32) NULL,
        [Name]          NVARCHAR(128) NOT NULL,
        [Address]       NVARCHAR(512) NULL,
        [Phone]         NVARCHAR(20) NULL,
        [ManagerName]   NVARCHAR(128) NULL,
        [IsActive]      BIT NOT NULL DEFAULT 1,
        [SortOrder]     INT NOT NULL DEFAULT 0,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Warehouses_StoreId] ON [koch].[Warehouses] ([StoreId]);
END
GO

-- کارت/موجودی کالا در انبار (در صورت نبود Product از ماژول محصولات می‌توان جدول موقت داشت)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'InventoryStock')
BEGIN
    CREATE TABLE [koch].[InventoryStock] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [WarehouseId]       INT NOT NULL,
        [ProductId]        INT NULL,
        [Sku]              NVARCHAR(64) NULL,
        [ItemName]         NVARCHAR(256) NULL,
        [Quantity]         DECIMAL(18,3) NOT NULL DEFAULT 0,
        [ReservedQuantity]  DECIMAL(18,3) NOT NULL DEFAULT 0,
        [MinQuantity]      DECIMAL(18,3) NULL,
        [MaxQuantity]      DECIMAL(18,3) NULL,
        [Unit]              NVARCHAR(16) NULL,
        [LocationInWarehouse] NVARCHAR(128) NULL,
        [LastMovementAt]    DATETIME2(2) NULL,
        [UpdatedAt]         DATETIME2(2) NULL,
        CONSTRAINT [PK_InventoryStock] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_InventoryStock_Warehouse] FOREIGN KEY ([WarehouseId]) REFERENCES [koch].[Warehouses]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_InventoryStock_Warehouse_Product] ON [koch].[InventoryStock] ([WarehouseId], [ProductId]) WHERE [ProductId] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [IX_InventoryStock_WarehouseId] ON [koch].[InventoryStock] ([WarehouseId]);
END
GO

-- سند حرکت انبار (رسید ورود، حواله خروج، انتقال، تعدیل)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'InventoryMovements')
BEGIN
    CREATE TABLE [koch].[InventoryMovements] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [WarehouseId]   INT NOT NULL,
        [Type]          NVARCHAR(32) NOT NULL,
        [ReferenceNo]   NVARCHAR(64) NULL,
        [MovementDate]  DATE NOT NULL,
        [Description]   NVARCHAR(512) NULL,
        [TargetWarehouseId] INT NULL,
        [CreatedBy]     INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [Status]        NVARCHAR(32) NULL,
        CONSTRAINT [PK_InventoryMovements] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_InventoryMovements_Warehouse] FOREIGN KEY ([WarehouseId]) REFERENCES [koch].[Warehouses]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_InventoryMovements_WarehouseId] ON [koch].[InventoryMovements] ([WarehouseId]);
    CREATE NONCLUSTERED INDEX [IX_InventoryMovements_Date] ON [koch].[InventoryMovements] ([MovementDate]);
END
GO

-- ریز حرکت (آیتم‌های هر سند)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'InventoryMovementLines')
BEGIN
    CREATE TABLE [koch].[InventoryMovementLines] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [MovementId]    INT NOT NULL,
        [ProductId]     INT NULL,
        [Sku]           NVARCHAR(64) NULL,
        [ItemName]      NVARCHAR(256) NULL,
        [Quantity]      DECIMAL(18,3) NOT NULL,
        [Unit]          NVARCHAR(16) NULL,
        [UnitPrice]     DECIMAL(18,2) NULL,
        [BatchNo]       NVARCHAR(64) NULL,
        [ExpiryDate]    DATE NULL,
        [Notes]         NVARCHAR(256) NULL,
        CONSTRAINT [PK_InventoryMovementLines] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_InventoryMovementLines_Movement] FOREIGN KEY ([MovementId]) REFERENCES [koch].[InventoryMovements]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_InventoryMovementLines_MovementId] ON [koch].[InventoryMovementLines] ([MovementId]);
END
GO
