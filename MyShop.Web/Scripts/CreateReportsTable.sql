-- ماژول گزارش‌های سفارشی: تعریف گزارش، پارامترها، اجرا و خروجی
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- تعریف گزارش
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'ReportDefinitions')
BEGIN
    CREATE TABLE [koch].[ReportDefinitions] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [Code]          NVARCHAR(64) NULL,
        [Name]          NVARCHAR(256) NOT NULL,
        [Description]   NVARCHAR(512) NULL,
        [ModuleKey]     NVARCHAR(64) NULL,
        [QueryOrConfig] NVARCHAR(MAX) NULL,
        [OutputType]    NVARCHAR(32) NULL,
        [ParametersJson] NVARCHAR(MAX) NULL,
        [IsActive]      BIT NOT NULL DEFAULT 1,
        [SortOrder]     INT NOT NULL DEFAULT 0,
        [CreatedBy]     INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_ReportDefinitions] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_ReportDefinitions_ModuleKey] ON [koch].[ReportDefinitions] ([ModuleKey]);
END
GO

-- سابقه اجرای گزارش
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'ReportRuns')
BEGIN
    CREATE TABLE [koch].[ReportRuns] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ReportId]      INT NOT NULL,
        [RunBy]         INT NULL,
        [RunAt]         DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [ParametersJson] NVARCHAR(MAX) NULL,
        [FileUrl]       NVARCHAR(1024) NULL,
        [RowCount]      INT NULL,
        [Status]        NVARCHAR(32) NULL,
        [ErrorMessage]  NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_ReportRuns] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ReportRuns_Report] FOREIGN KEY ([ReportId]) REFERENCES [koch].[ReportDefinitions]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ReportRuns_ReportId] ON [koch].[ReportRuns] ([ReportId]);
    CREATE NONCLUSTERED INDEX [IX_ReportRuns_RunAt] ON [koch].[ReportRuns] ([RunAt]);
END
GO
