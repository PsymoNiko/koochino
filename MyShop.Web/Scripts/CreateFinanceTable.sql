-- ماژول حسابداری: حساب‌ها، تراکنش‌ها، ریز تراکنش (بدهکار/بستانکار)
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- حساب‌ها (معین/کل)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'FinanceAccounts')
BEGIN
    CREATE TABLE [koch].[FinanceAccounts] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [StoreId]       INT NULL,
        [ParentId]      INT NULL,
        [Code]          NVARCHAR(32) NOT NULL,
        [Name]          NVARCHAR(256) NOT NULL,
        [Type]          NVARCHAR(32) NULL,
        [Balance]       DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Currency]      NVARCHAR(8) NULL,
        [IsActive]      BIT NOT NULL DEFAULT 1,
        [SortOrder]     INT NOT NULL DEFAULT 0,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_FinanceAccounts] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FinanceAccounts_Parent] FOREIGN KEY ([ParentId]) REFERENCES [koch].[FinanceAccounts]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_FinanceAccounts_StoreId] ON [koch].[FinanceAccounts] ([StoreId]);
    CREATE NONCLUSTERED INDEX [IX_FinanceAccounts_ParentId] ON [koch].[FinanceAccounts] ([ParentId]);
END
GO

-- سند/تراکنش مالی
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'FinanceTransactions')
BEGIN
    CREATE TABLE [koch].[FinanceTransactions] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [StoreId]       INT NULL,
        [DocumentNo]    NVARCHAR(64) NULL,
        [TransactionDate] DATE NOT NULL,
        [Description]   NVARCHAR(512) NULL,
        [Reference]     NVARCHAR(128) NULL,
        [TotalDebit]    DECIMAL(18,2) NOT NULL DEFAULT 0,
        [TotalCredit]   DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Status]        NVARCHAR(32) NULL,
        [CreatedBy]     INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_FinanceTransactions] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_FinanceTransactions_StoreId] ON [koch].[FinanceTransactions] ([StoreId]);
    CREATE NONCLUSTERED INDEX [IX_FinanceTransactions_Date] ON [koch].[FinanceTransactions] ([TransactionDate]);
END
GO

-- ریز سند (بدهکار/بستانکار)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'FinanceTransactionLines')
BEGIN
    CREATE TABLE [koch].[FinanceTransactionLines] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [TransactionId] INT NOT NULL,
        [AccountId]     INT NOT NULL,
        [Debit]         DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Credit]        DECIMAL(18,2) NOT NULL DEFAULT 0,
        [Description]   NVARCHAR(256) NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_FinanceTransactionLines] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FinanceTransactionLines_Transaction] FOREIGN KEY ([TransactionId]) REFERENCES [koch].[FinanceTransactions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FinanceTransactionLines_Account] FOREIGN KEY ([AccountId]) REFERENCES [koch].[FinanceAccounts]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_FinanceTransactionLines_TransactionId] ON [koch].[FinanceTransactionLines] ([TransactionId]);
    CREATE NONCLUSTERED INDEX [IX_FinanceTransactionLines_AccountId] ON [koch].[FinanceTransactionLines] ([AccountId]);
END
GO
