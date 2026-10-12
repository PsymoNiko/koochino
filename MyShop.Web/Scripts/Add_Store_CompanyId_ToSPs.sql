-- اضافه کردن CompanyId به SPهای فروشگاه (اجرا بعد از CreateCompaniesAndHoldingsTables.sql)
USE [koch]
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('koch.Stores') AND name = 'CompanyId')
    ALTER TABLE [koch].[Stores] ADD [CompanyId] INT NULL;
GO

-- به‌روزرسانی spStores_Update با پارامتر CompanyId
IF OBJECT_ID('koch.spStores_Update', 'P') IS NOT NULL DROP PROCEDURE [koch].[spStores_Update];
GO
CREATE PROCEDURE [koch].[spStores_Update]
    @StoreId INT,
    @StoreName NVARCHAR(200),
    @StoreDescription NVARCHAR(MAX) = NULL,
    @StoreAddress NVARCHAR(500) = NULL,
    @StoreFamilyName NVARCHAR(50) = NULL,
    @StoreCity INT = NULL,
    @StorePostCode NVARCHAR(50) = NULL,
    @StorePhone NVARCHAR(50) = NULL,
    @StorePos1 NVARCHAR(50) = NULL,
    @StorePos2 NVARCHAR(50) = NULL,
    @StorePos3 NVARCHAR(50) = NULL,
    @StoreLatitude DECIMAL(10, 8) = NULL,
    @StoreLongitude DECIMAL(11, 8) = NULL,
    @StoreProfileImageUrl NVARCHAR(500) = NULL,
    @StoreOpeningHours NVARCHAR(10) = NULL,
    @StoreClosingHours NVARCHAR(10) = NULL,
    @StoreWorkingDays NVARCHAR(100) = NULL,
    @StoreWebsite NVARCHAR(255) = NULL,
    @StoreSocialMedia NVARCHAR(MAX) = NULL,
    @IsLocationEnabled BIT = NULL,
    @CompanyId INT = NULL,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [koch].[Stores] WHERE [StoreID] = @StoreId)
    BEGIN
        SET @Result = 0;
        RETURN;
    END
    UPDATE [koch].[Stores]
    SET
        [StoreName] = @StoreName,
        [StoreDescription] = @StoreDescription,
        [StoreAddress] = @StoreAddress,
        [StoreFamilyName] = @StoreFamilyName,
        [StoreCity] = @StoreCity,
        [StorePostCode] = @StorePostCode,
        [StorePhone] = @StorePhone,
        [StorePos1] = @StorePos1,
        [StorePos2] = @StorePos2,
        [StorePos3] = @StorePos3,
        [StoreLatitude] = @StoreLatitude,
        [StoreLongitude] = @StoreLongitude,
        [StoreProfileImageUrl] = @StoreProfileImageUrl,
        [StoreOpeningHours] = @StoreOpeningHours,
        [StoreClosingHours] = @StoreClosingHours,
        [StoreWorkingDays] = @StoreWorkingDays,
        [StoreWebsite] = @StoreWebsite,
        [StoreSocialMedia] = @StoreSocialMedia,
        [IsLocationEnabled] = ISNULL(@IsLocationEnabled, [IsLocationEnabled]),
        [CompanyId] = @CompanyId
    WHERE [StoreID] = @StoreId;
    SET @Result = 1;
END
GO

-- spStores_Create: اضافه کردن @CompanyId و ستون CompanyId در INSERT
IF OBJECT_ID('koch.spStores_Create', 'P') IS NOT NULL DROP PROCEDURE [koch].[spStores_Create];
GO
CREATE PROCEDURE [koch].[spStores_Create]
    @UserId INT,
    @StoreName NVARCHAR(200),
    @StoreDescription NVARCHAR(MAX) = NULL,
    @StoreAddress NVARCHAR(500) = NULL,
    @StoreFamilyName NVARCHAR(50) = NULL,
    @StoreCity INT = NULL,
    @StorePostCode NVARCHAR(50) = NULL,
    @StorePhone NVARCHAR(50) = NULL,
    @StorePos1 NVARCHAR(50) = NULL,
    @StorePos2 NVARCHAR(50) = NULL,
    @StorePos3 NVARCHAR(50) = NULL,
    @CompanyId INT = NULL,
    @StoreId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [koch].[Stores]
    (
        [UserID], [StoreName], [StoreDescription], [StoreAddress], [StoreCreatedAt],
        [StoreFamilyName], [StoreCity], [StorePostCode], [StorePhone],
        [StorePos1], [StorePos2], [StorePos3], [CompanyId]
    )
    VALUES
    (
        @UserId, @StoreName, @StoreDescription, @StoreAddress, SYSDATETIME(),
        @StoreFamilyName, @StoreCity, @StorePostCode, @StorePhone,
        @StorePos1, @StorePos2, @StorePos3, @CompanyId
    );
    SET @StoreId = SCOPE_IDENTITY();
END
GO

PRINT 'CompanyId به SPهای فروشگاه اضافه شد.';
GO
