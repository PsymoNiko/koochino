-- ماژول اسلایدر: جداول Sliders و SliderSlides + Stored Procedures
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- جدول اسلایدرها (هر اسلایدر برای یک محل در سایت)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Sliders')
BEGIN
    CREATE TABLE [koch].[Sliders] (
        [Id]          INT IDENTITY(1,1) NOT NULL,
        [Name]        NVARCHAR(128) NOT NULL,
        [PlaceKey]    NVARCHAR(64)  NOT NULL,   -- کد محل استفاده: مثلاً Homepage, Category, Sidebar
        [Description] NVARCHAR(512) NULL,
        [IsActive]    BIT NOT NULL DEFAULT 1,
        [SortOrder]   INT NOT NULL DEFAULT 0,
        [CreatedAt]   DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]   DATETIME2(2) NULL,
        CONSTRAINT [PK_Sliders] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE UNIQUE INDEX [IX_Sliders_PlaceKey] ON [koch].[Sliders] ([PlaceKey]);
END
GO

-- جدول اسلایدها (هر اسلایدر چند اسلاید دارد)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'SliderSlides')
BEGIN
    CREATE TABLE [koch].[SliderSlides] (
        [Id]           INT IDENTITY(1,1) NOT NULL,
        [SliderId]     INT NOT NULL,
        [Title]        NVARCHAR(256) NULL,
        [Subtitle]     NVARCHAR(256) NULL,
        [Description]  NVARCHAR(MAX) NULL,
        [ImageUrl]     NVARCHAR(1024) NULL,
        [ImageAltText] NVARCHAR(256) NULL,
        [LinkUrl]      NVARCHAR(1024) NULL,
        [LinkTarget]    NVARCHAR(16)  NULL DEFAULT '_self',  -- _self, _blank
        [ButtonText]   NVARCHAR(128) NULL,
        [SortOrder]    INT NOT NULL DEFAULT 0,
        [IsActive]     BIT NOT NULL DEFAULT 1,
        [StartDate]    DATETIME2(2) NULL,
        [EndDate]      DATETIME2(2) NULL,
        [CreatedAt]    DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]    DATETIME2(2) NULL,
        CONSTRAINT [PK_SliderSlides] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_SliderSlides_Slider] FOREIGN KEY ([SliderId]) REFERENCES [koch].[Sliders]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_SliderSlides_SliderId] ON [koch].[SliderSlides] ([SliderId]);
END
GO

-- ========== Stored Procedures: Sliders ==========
IF OBJECT_ID('koch.spSliders_List', 'P') IS NOT NULL DROP PROCEDURE koch.spSliders_List;
GO
CREATE PROCEDURE koch.spSliders_List @PlaceKey NVARCHAR(64) = NULL, @IsActive BIT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, Name, PlaceKey, Description, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Sliders
    WHERE (@PlaceKey IS NULL OR PlaceKey = @PlaceKey)
      AND (@IsActive IS NULL OR IsActive = @IsActive)
    ORDER BY SortOrder, Id;
END
GO

IF OBJECT_ID('koch.spSliders_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spSliders_GetById;
GO
CREATE PROCEDURE koch.spSliders_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, Name, PlaceKey, Description, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Sliders WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spSliders_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spSliders_Insert;
GO
CREATE PROCEDURE koch.spSliders_Insert
    @Name NVARCHAR(128),
    @PlaceKey NVARCHAR(64),
    @Description NVARCHAR(512) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.Sliders (Name, PlaceKey, Description, IsActive, SortOrder, CreatedAt)
    VALUES (@Name, @PlaceKey, @Description, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spSliders_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spSliders_Update;
GO
CREATE PROCEDURE koch.spSliders_Update
    @Id INT,
    @Name NVARCHAR(128),
    @PlaceKey NVARCHAR(64),
    @Description NVARCHAR(512) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.Sliders
    SET Name = @Name, PlaceKey = @PlaceKey, Description = @Description, IsActive = @IsActive, SortOrder = @SortOrder, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spSliders_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spSliders_Delete;
GO
CREATE PROCEDURE koch.spSliders_Delete @Id INT, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    DELETE FROM koch.SliderSlides WHERE SliderId = @Id;
    DELETE FROM koch.Sliders WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- ========== Stored Procedures: SliderSlides ==========
IF OBJECT_ID('koch.spSliderSlides_ListBySlider', 'P') IS NOT NULL DROP PROCEDURE koch.spSliderSlides_ListBySlider;
GO
CREATE PROCEDURE koch.spSliderSlides_ListBySlider @SliderId INT, @IsActive BIT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, SliderId, Title, Subtitle, Description, ImageUrl, ImageAltText, LinkUrl, LinkTarget, ButtonText, SortOrder, IsActive, StartDate, EndDate, CreatedAt, UpdatedAt
    FROM koch.SliderSlides
    WHERE SliderId = @SliderId AND (@IsActive IS NULL OR IsActive = @IsActive)
    ORDER BY SortOrder, Id;
END
GO

-- برای نمایش در سایت: اسلایدهای فعال یک اسلایدر در بازه زمانی
IF OBJECT_ID('koch.spSliderSlides_GetActiveForPlace', 'P') IS NOT NULL DROP PROCEDURE koch.spSliderSlides_GetActiveForPlace;
GO
CREATE PROCEDURE koch.spSliderSlides_GetActiveForPlace @PlaceKey NVARCHAR(64)
AS
BEGIN SET NOCOUNT ON;
    DECLARE @Now DATETIME2(2) = SYSDATETIME();
    SELECT s.Id, s.SliderId, s.Title, s.Subtitle, s.Description, s.ImageUrl, s.ImageAltText, s.LinkUrl, s.LinkTarget, s.ButtonText, s.SortOrder
    FROM koch.SliderSlides s
    INNER JOIN koch.Sliders r ON r.Id = s.SliderId
    WHERE r.PlaceKey = @PlaceKey AND r.IsActive = 1 AND s.IsActive = 1
      AND (s.StartDate IS NULL OR s.StartDate <= @Now)
      AND (s.EndDate IS NULL OR s.EndDate >= @Now)
    ORDER BY r.SortOrder, r.Id, s.SortOrder, s.Id;
END
GO

IF OBJECT_ID('koch.spSliderSlides_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spSliderSlides_GetById;
GO
CREATE PROCEDURE koch.spSliderSlides_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, SliderId, Title, Subtitle, Description, ImageUrl, ImageAltText, LinkUrl, LinkTarget, ButtonText, SortOrder, IsActive, StartDate, EndDate, CreatedAt, UpdatedAt
    FROM koch.SliderSlides WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spSliderSlides_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spSliderSlides_Insert;
GO
CREATE PROCEDURE koch.spSliderSlides_Insert
    @SliderId INT,
    @Title NVARCHAR(256) = NULL,
    @Subtitle NVARCHAR(256) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL,
    @ImageAltText NVARCHAR(256) = NULL,
    @LinkUrl NVARCHAR(1024) = NULL,
    @LinkTarget NVARCHAR(16) = '_self',
    @ButtonText NVARCHAR(128) = NULL,
    @SortOrder INT = 0,
    @IsActive BIT = 1,
    @StartDate DATETIME2(2) = NULL,
    @EndDate DATETIME2(2) = NULL,
    @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.SliderSlides (SliderId, Title, Subtitle, Description, ImageUrl, ImageAltText, LinkUrl, LinkTarget, ButtonText, SortOrder, IsActive, StartDate, EndDate, CreatedAt)
    VALUES (@SliderId, @Title, @Subtitle, @Description, @ImageUrl, @ImageAltText, @LinkUrl, @LinkTarget, @ButtonText, @SortOrder, @IsActive, @StartDate, @EndDate, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spSliderSlides_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spSliderSlides_Update;
GO
CREATE PROCEDURE koch.spSliderSlides_Update
    @Id INT,
    @SliderId INT,
    @Title NVARCHAR(256) = NULL,
    @Subtitle NVARCHAR(256) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL,
    @ImageAltText NVARCHAR(256) = NULL,
    @LinkUrl NVARCHAR(1024) = NULL,
    @LinkTarget NVARCHAR(16) = '_self',
    @ButtonText NVARCHAR(128) = NULL,
    @SortOrder INT = 0,
    @IsActive BIT = 1,
    @StartDate DATETIME2(2) = NULL,
    @EndDate DATETIME2(2) = NULL,
    @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.SliderSlides
    SET SliderId = @SliderId, Title = @Title, Subtitle = @Subtitle, Description = @Description,
        ImageUrl = @ImageUrl, ImageAltText = @ImageAltText, LinkUrl = @LinkUrl, LinkTarget = @LinkTarget,
        ButtonText = @ButtonText, SortOrder = @SortOrder, IsActive = @IsActive, StartDate = @StartDate, EndDate = @EndDate, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spSliderSlides_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spSliderSlides_Delete;
GO
CREATE PROCEDURE koch.spSliderSlides_Delete @Id INT, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    DELETE FROM koch.SliderSlides WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
