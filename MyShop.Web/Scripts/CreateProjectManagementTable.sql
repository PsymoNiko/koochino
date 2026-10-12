-- ماژول مدیریت پروژه: تعریف پروژه، اعضای تیم، وظایف، مایل‌استون، نظرات، پیگیری
-- اسکریپت را روی دیتابیس مورد نظر اجرا کنید.

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch')
    EXEC('CREATE SCHEMA koch');
GO

-- جدول پروژه‌ها
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Projects')
BEGIN
    CREATE TABLE [koch].[Projects] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [StoreId]       INT NULL,
        [Title]         NVARCHAR(256) NOT NULL,
        [Description]   NVARCHAR(MAX) NULL,
        [Status]        NVARCHAR(32) NOT NULL DEFAULT N'Planning',
        [Priority]      NVARCHAR(32) NULL,
        [StartDate]     DATE NULL,
        [EndDate]       DATE NULL,
        [Progress]      TINYINT NOT NULL DEFAULT 0,
        [CreatedBy]     INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Projects_StoreId] ON [koch].[Projects] ([StoreId]);
    CREATE NONCLUSTERED INDEX [IX_Projects_Status] ON [koch].[Projects] ([Status]);
END
GO

-- اعضای پروژه (افرادی که پروژه را انجام می‌دهند)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'ProjectMembers')
BEGIN
    CREATE TABLE [koch].[ProjectMembers] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ProjectId]     INT NOT NULL,
        [UserId]        INT NULL,
        [PersonnelId]   INT NULL,
        [Role]          NVARCHAR(64) NULL,
        [JoinedAt]      DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_ProjectMembers] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProjectMembers_Project] FOREIGN KEY ([ProjectId]) REFERENCES [koch].[Projects]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProjectMembers_Project_User] ON [koch].[ProjectMembers] ([ProjectId], [UserId]) WHERE [UserId] IS NOT NULL;
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ProjectMembers_Project_Personnel] ON [koch].[ProjectMembers] ([ProjectId], [PersonnelId]) WHERE [PersonnelId] IS NOT NULL;
END
GO

-- وظایف پروژه
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'ProjectTasks')
BEGIN
    CREATE TABLE [koch].[ProjectTasks] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ProjectId]     INT NOT NULL,
        [ParentTaskId]  INT NULL,
        [Title]         NVARCHAR(256) NOT NULL,
        [Description]   NVARCHAR(MAX) NULL,
        [AssigneeId]    INT NULL,
        [PersonnelId]   INT NULL,
        [Status]        NVARCHAR(32) NOT NULL DEFAULT N'Todo',
        [Priority]      NVARCHAR(32) NULL,
        [DueDate]       DATE NULL,
        [CompletedAt]   DATETIME2(2) NULL,
        [SortOrder]     INT NOT NULL DEFAULT 0,
        [Progress]      TINYINT NOT NULL DEFAULT 0,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_ProjectTasks] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProjectTasks_Project] FOREIGN KEY ([ProjectId]) REFERENCES [koch].[Projects]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectTasks_Parent] FOREIGN KEY ([ParentTaskId]) REFERENCES [koch].[ProjectTasks]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectTasks_ProjectId] ON [koch].[ProjectTasks] ([ProjectId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectTasks_Status] ON [koch].[ProjectTasks] ([Status]);
END
GO

-- مایل‌استون‌های پروژه
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'ProjectMilestones')
BEGIN
    CREATE TABLE [koch].[ProjectMilestones] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ProjectId]     INT NOT NULL,
        [Title]         NVARCHAR(256) NOT NULL,
        [DueDate]       DATE NULL,
        [CompletedAt]   DATETIME2(2) NULL,
        [SortOrder]     INT NOT NULL DEFAULT 0,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_ProjectMilestones] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProjectMilestones_Project] FOREIGN KEY ([ProjectId]) REFERENCES [koch].[Projects]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectMilestones_ProjectId] ON [koch].[ProjectMilestones] ([ProjectId]);
END
GO

-- نظرات و پیگیری پروژه / وظیفه
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'ProjectComments')
BEGIN
    CREATE TABLE [koch].[ProjectComments] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ProjectId]     INT NOT NULL,
        [TaskId]        INT NULL,
        [UserId]        INT NULL,
        [Content]       NVARCHAR(MAX) NOT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_ProjectComments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProjectComments_Project] FOREIGN KEY ([ProjectId]) REFERENCES [koch].[Projects]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectComments_Task] FOREIGN KEY ([TaskId]) REFERENCES [koch].[ProjectTasks]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectComments_ProjectId] ON [koch].[ProjectComments] ([ProjectId]);
    CREATE NONCLUSTERED INDEX [IX_ProjectComments_TaskId] ON [koch].[ProjectComments] ([TaskId]);
END
GO

-- اسناد و فایل‌های پیوست پروژه / وظیفه
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'ProjectDocuments')
BEGIN
    CREATE TABLE [koch].[ProjectDocuments] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [ProjectId]     INT NOT NULL,
        [TaskId]        INT NULL,
        [FileUrl]       NVARCHAR(1024) NOT NULL,
        [FileName]      NVARCHAR(256) NULL,
        [UploadedBy]    INT NULL,
        [UploadedAt]    DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_ProjectDocuments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProjectDocuments_Project] FOREIGN KEY ([ProjectId]) REFERENCES [koch].[Projects]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectDocuments_Task] FOREIGN KEY ([TaskId]) REFERENCES [koch].[ProjectTasks]([Id]) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX [IX_ProjectDocuments_ProjectId] ON [koch].[ProjectDocuments] ([ProjectId]);
END
GO
