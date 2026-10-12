-- ماژول پرسونل: تعریف پرسنل شرکت/فروشگاه با رزومه، ساعت کاری، مشخصات و ...
-- اسکریپت را روی دیتابیس مورد نظر اجرا کنید.

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch')
    EXEC('CREATE SCHEMA koch');
GO

-- جدول اصلی پرسنل (هر آنچه یک پرسنل نیاز دارد)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Personnel')
BEGIN
    CREATE TABLE [koch].[Personnel] (
        [Id]                    INT IDENTITY(1,1) NOT NULL,
        [StoreId]               INT NULL,
        [FullName]              NVARCHAR(128) NOT NULL,
        [NationalCode]          NVARCHAR(10) NULL,
        [Phone]                 NVARCHAR(20) NULL,
        [Email]                 NVARCHAR(256) NULL,
        [Position]              NVARCHAR(128) NULL,
        [Department]            NVARCHAR(128) NULL,
        [ResumeText]             NVARCHAR(MAX) NULL,
        [ResumeFileUrl]          NVARCHAR(1024) NULL,
        [HireDate]              DATE NULL,
        [LeaveDate]              DATE NULL,
        [BirthDate]             DATE NULL,
        [Address]               NVARCHAR(512) NULL,
        [Specifications]        NVARCHAR(MAX) NULL,
        [DefaultWorkStartTime]   TIME(0) NULL,
        [DefaultWorkEndTime]     TIME(0) NULL,
        [ImageUrl]              NVARCHAR(1024) NULL,
        [IsActive]              BIT NOT NULL DEFAULT 1,
        [SortOrder]             INT NOT NULL DEFAULT 0,
        [CreatedAt]             DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]             DATETIME2(2) NULL,
        CONSTRAINT [PK_Personnel] PRIMARY KEY CLUSTERED ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Personnel_StoreId] ON [koch].[Personnel] ([StoreId]);
    CREATE NONCLUSTERED INDEX [IX_Personnel_IsActive_SortOrder] ON [koch].[Personnel] ([IsActive], [SortOrder]);
END
GO

-- ساعت کاری به تفکیک روز (شنبه=1 تا جمعه=7) — در صورت نیاز به برنامه هفتگی متفاوت
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'PersonnelSchedule')
BEGIN
    CREATE TABLE [koch].[PersonnelSchedule] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [PersonnelId]   INT NOT NULL,
        [DayOfWeek]     TINYINT NOT NULL,
        [StartTime]     TIME(0) NULL,
        [EndTime]       TIME(0) NULL,
        [IsOff]         BIT NOT NULL DEFAULT 0,
        [Notes]         NVARCHAR(256) NULL,
        CONSTRAINT [PK_PersonnelSchedule] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PersonnelSchedule_Personnel] FOREIGN KEY ([PersonnelId]) REFERENCES [koch].[Personnel]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_PersonnelSchedule_PersonnelId] ON [koch].[PersonnelSchedule] ([PersonnelId]);
END
GO
