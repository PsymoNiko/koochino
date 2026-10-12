-- ماژول CRM: مشتریان/مخاطبین، پیگیری، تاریخچه تماس و فعالیت
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- مخاطبین / مشتریان CRM
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'CrmContacts')
BEGIN
    CREATE TABLE [koch].[CrmContacts] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [StoreId]       INT NULL,
        [FullName]      NVARCHAR(128) NOT NULL,
        [Phone]         NVARCHAR(20) NULL,
        [Phone2]        NVARCHAR(20) NULL,
        [Email]         NVARCHAR(256) NULL,
        [Company]       NVARCHAR(256) NULL,
        [JobTitle]      NVARCHAR(128) NULL,
        [Address]       NVARCHAR(512) NULL,
        [City]          NVARCHAR(64) NULL,
        [Province]      NVARCHAR(64) NULL,
        [PostalCode]    NVARCHAR(20) NULL,
        [Source]        NVARCHAR(64) NULL,
        [Tags]          NVARCHAR(512) NULL,
        [Notes]         NVARCHAR(MAX) NULL,
        [AssignedTo]    INT NULL,
        [Status]        NVARCHAR(32) NULL,
        [Rating]        TINYINT NULL,
        [IsActive]      BIT NOT NULL DEFAULT 1,
        [CreatedBy]     INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        [LastActivityAt] DATETIME2(2) NULL,
        CONSTRAINT [PK_CrmContacts] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_CrmContacts_StoreId] ON [koch].[CrmContacts] ([StoreId]);
    CREATE NONCLUSTERED INDEX [IX_CrmContacts_Phone] ON [koch].[CrmContacts] ([Phone]);
    CREATE NONCLUSTERED INDEX [IX_CrmContacts_Email] ON [koch].[CrmContacts] ([Email]);
END
GO

-- فعالیت‌ها و تاریخچه (تماس، جلسه، ایمیل، یادداشت)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'CrmActivities')
BEGIN
    CREATE TABLE [koch].[CrmActivities] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ContactId]     INT NOT NULL,
        [Type]          NVARCHAR(32) NOT NULL,
        [Subject]       NVARCHAR(256) NULL,
        [Description]   NVARCHAR(MAX) NULL,
        [ActivityDate]  DATETIME2(2) NULL,
        [DueDate]       DATETIME2(2) NULL,
        [DoneAt]        DATETIME2(2) NULL,
        [DurationMinutes] INT NULL,
        [CreatedBy]     INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_CrmActivities] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_CrmActivities_Contact] FOREIGN KEY ([ContactId]) REFERENCES [koch].[CrmContacts]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_CrmActivities_ContactId] ON [koch].[CrmActivities] ([ContactId]);
    CREATE NONCLUSTERED INDEX [IX_CrmActivities_ActivityDate] ON [koch].[CrmActivities] ([ActivityDate]);
END
GO
