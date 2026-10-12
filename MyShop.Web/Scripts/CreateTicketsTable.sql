-- ماژول تیکت پشتیبانی: دپارتمان، تیکت، پاسخ، پیوست
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- دپارتمان/دسته تیکت
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'TicketDepartments')
BEGIN
    CREATE TABLE [koch].[TicketDepartments] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [Name]          NVARCHAR(128) NOT NULL,
        [Description]   NVARCHAR(512) NULL,
        [Email]         NVARCHAR(256) NULL,
        [IsActive]      BIT NOT NULL DEFAULT 1,
        [SortOrder]     INT NOT NULL DEFAULT 0,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_TicketDepartments] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

-- تیکت‌ها
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Tickets')
BEGIN
    CREATE TABLE [koch].[Tickets] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [DepartmentId]  INT NULL,
        [TicketNo]      NVARCHAR(32) NULL,
        [UserId]        INT NULL,
        [ContactName]   NVARCHAR(128) NULL,
        [ContactEmail]  NVARCHAR(256) NULL,
        [ContactPhone]  NVARCHAR(20) NULL,
        [Subject]       NVARCHAR(256) NOT NULL,
        [Status]        NVARCHAR(32) NOT NULL DEFAULT N'Open',
        [Priority]      NVARCHAR(32) NULL,
        [AssignedTo]    INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        [ClosedAt]      DATETIME2(2) NULL,
        [ClosedBy]      INT NULL,
        [Resolution]    NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Tickets_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [koch].[TicketDepartments]([Id]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_Tickets_DepartmentId] ON [koch].[Tickets] ([DepartmentId]);
    CREATE NONCLUSTERED INDEX [IX_Tickets_Status] ON [koch].[Tickets] ([Status]);
    CREATE NONCLUSTERED INDEX [IX_Tickets_UserId] ON [koch].[Tickets] ([UserId]);
END
GO

-- پاسخ‌های تیکت
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'TicketReplies')
BEGIN
    CREATE TABLE [koch].[TicketReplies] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [TicketId]      INT NOT NULL,
        [UserId]        INT NULL,
        [Content]       NVARCHAR(MAX) NOT NULL,
        [IsStaffReply]  BIT NOT NULL DEFAULT 0,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_TicketReplies] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TicketReplies_Ticket] FOREIGN KEY ([TicketId]) REFERENCES [koch].[Tickets]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_TicketReplies_TicketId] ON [koch].[TicketReplies] ([TicketId]);
END
GO

-- پیوست تیکت/پاسخ
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'TicketAttachments')
BEGIN
    CREATE TABLE [koch].[TicketAttachments] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [TicketId]      INT NOT NULL,
        [ReplyId]       INT NULL,
        [FileUrl]       NVARCHAR(1024) NOT NULL,
        [FileName]      NVARCHAR(256) NULL,
        [FileSize]      INT NULL,
        [UploadedBy]    INT NULL,
        [UploadedAt]    DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_TicketAttachments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TicketAttachments_Ticket] FOREIGN KEY ([TicketId]) REFERENCES [koch].[Tickets]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TicketAttachments_Reply] FOREIGN KEY ([ReplyId]) REFERENCES [koch].[TicketReplies]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_TicketAttachments_TicketId] ON [koch].[TicketAttachments] ([TicketId]);
END
GO
