-- ماژول آموزش: دوره‌ها، جلسات، ثبت‌نام، گواهی
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- دوره‌های آموزشی
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'TrainingCourses')
BEGIN
    CREATE TABLE [koch].[TrainingCourses] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [StoreId]           INT NULL,
        [Code]              NVARCHAR(32) NULL,
        [Title]             NVARCHAR(256) NOT NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [DurationHours]     DECIMAL(6,2) NULL,
        [StartDate]         DATE NULL,
        [EndDate]           DATE NULL,
        [MaxParticipants]   INT NULL,
        [Location]          NVARCHAR(256) NULL,
        [InstructorName]    NVARCHAR(128) NULL,
        [ImageUrl]          NVARCHAR(1024) NULL,
        [IsActive]          BIT NOT NULL DEFAULT 1,
        [SortOrder]         INT NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]         DATETIME2(2) NULL,
        CONSTRAINT [PK_TrainingCourses] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_TrainingCourses_StoreId] ON [koch].[TrainingCourses] ([StoreId]);
END
GO

-- جلسات دوره (در صورت نیاز به چند جلسه)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'TrainingSessions')
BEGIN
    CREATE TABLE [koch].[TrainingSessions] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [CourseId]      INT NOT NULL,
        [SessionDate]   DATE NOT NULL,
        [StartTime]     TIME(0) NULL,
        [EndTime]       TIME(0) NULL,
        [Title]         NVARCHAR(256) NULL,
        [Location]      NVARCHAR(256) NULL,
        [Notes]         NVARCHAR(MAX) NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_TrainingSessions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TrainingSessions_Course] FOREIGN KEY ([CourseId]) REFERENCES [koch].[TrainingCourses]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_TrainingSessions_CourseId] ON [koch].[TrainingSessions] ([CourseId]);
END
GO

-- ثبت‌نام در دوره
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'TrainingEnrollments')
BEGIN
    CREATE TABLE [koch].[TrainingEnrollments] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [CourseId]          INT NOT NULL,
        [UserId]            INT NULL,
        [PersonnelId]       INT NULL,
        [EnrolledAt]        DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [Status]            NVARCHAR(32) NULL,
        [CompletedAt]       DATETIME2(2) NULL,
        [Score]             DECIMAL(5,2) NULL,
        [CertificateUrl]    NVARCHAR(1024) NULL,
        [CertificateNo]     NVARCHAR(64) NULL,
        [Notes]             NVARCHAR(MAX) NULL,
        [CreatedAt]         DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]         DATETIME2(2) NULL,
        CONSTRAINT [PK_TrainingEnrollments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TrainingEnrollments_Course] FOREIGN KEY ([CourseId]) REFERENCES [koch].[TrainingCourses]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_TrainingEnrollments_CourseId] ON [koch].[TrainingEnrollments] ([CourseId]);
    CREATE NONCLUSTERED INDEX [IX_TrainingEnrollments_UserId] ON [koch].[TrainingEnrollments] ([UserId]);
    CREATE NONCLUSTERED INDEX [IX_TrainingEnrollments_PersonnelId] ON [koch].[TrainingEnrollments] ([PersonnelId]);
END
GO
