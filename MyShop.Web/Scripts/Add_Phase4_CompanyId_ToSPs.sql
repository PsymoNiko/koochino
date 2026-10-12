-- فاز ۴: اضافه کردن CompanyId به SPهای دارایی، مالی، CRM، قرارداد، پروژه، محصول، آموزش
-- اجرا بعد از CreateCompaniesAndHoldingsTables.sql (ستون CompanyId از قبل در جداول اضافه شده)
USE [koch]
GO

-- ========== دارایی (Assets) ==========
IF OBJECT_ID('koch.spAssets_List', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_List;
GO
CREATE PROCEDURE koch.spAssets_List
    @StoreId INT = NULL,
    @CompanyId INT = NULL,
    @CategoryId INT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, CategoryId, Code, Name, Description, PurchaseDate, PurchasePrice, CurrentValue, Location, AssignedTo, PersonnelId, Status, WarrantyEndDate, DepreciationRate, ImageUrl, SerialNo, Notes, IsActive, CreatedAt, UpdatedAt
    FROM koch.Assets
    WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@CompanyId IS NULL OR CompanyId = @CompanyId) AND (@CategoryId IS NULL OR CategoryId = @CategoryId)
    ORDER BY Name;
END
GO

IF OBJECT_ID('koch.spAssets_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_GetById;
GO
CREATE PROCEDURE koch.spAssets_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, CategoryId, Code, Name, Description, PurchaseDate, PurchasePrice, CurrentValue, Location, AssignedTo, PersonnelId, Status, WarrantyEndDate, DepreciationRate, ImageUrl, SerialNo, Notes, IsActive, CreatedAt, UpdatedAt
    FROM koch.Assets WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spAssets_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_Insert;
GO
CREATE PROCEDURE koch.spAssets_Insert
    @StoreId INT = NULL, @CompanyId INT = NULL, @CategoryId INT = NULL, @Code NVARCHAR(64) = NULL, @Name NVARCHAR(256),
    @Description NVARCHAR(MAX) = NULL, @PurchaseDate DATE = NULL, @PurchasePrice DECIMAL(18,2) = NULL, @CurrentValue DECIMAL(18,2) = NULL, @Location NVARCHAR(256) = NULL,
    @AssignedTo INT = NULL, @PersonnelId INT = NULL, @Status NVARCHAR(32) = NULL, @WarrantyEndDate DATE = NULL, @DepreciationRate DECIMAL(5,2) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL, @SerialNo NVARCHAR(128) = NULL, @Notes NVARCHAR(MAX) = NULL, @IsActive BIT = 1, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.Assets (StoreId, CompanyId, CategoryId, Code, Name, Description, PurchaseDate, PurchasePrice, CurrentValue, Location, AssignedTo, PersonnelId, Status, WarrantyEndDate, DepreciationRate, ImageUrl, SerialNo, Notes, IsActive, CreatedAt)
    VALUES (@StoreId, @CompanyId, @CategoryId, @Code, @Name, @Description, @PurchaseDate, @PurchasePrice, @CurrentValue, @Location, @AssignedTo, @PersonnelId, @Status, @WarrantyEndDate, @DepreciationRate, @ImageUrl, @SerialNo, @Notes, @IsActive, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spAssets_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spAssets_Update;
GO
CREATE PROCEDURE koch.spAssets_Update
    @Id INT, @StoreId INT = NULL, @CompanyId INT = NULL, @CategoryId INT = NULL, @Code NVARCHAR(64) = NULL, @Name NVARCHAR(256),
    @Description NVARCHAR(MAX) = NULL, @PurchaseDate DATE = NULL, @PurchasePrice DECIMAL(18,2) = NULL, @CurrentValue DECIMAL(18,2) = NULL, @Location NVARCHAR(256) = NULL,
    @AssignedTo INT = NULL, @PersonnelId INT = NULL, @Status NVARCHAR(32) = NULL, @WarrantyEndDate DATE = NULL, @DepreciationRate DECIMAL(5,2) = NULL,
    @ImageUrl NVARCHAR(1024) = NULL, @SerialNo NVARCHAR(128) = NULL, @Notes NVARCHAR(MAX) = NULL, @IsActive BIT = 1, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.Assets SET StoreId=@StoreId, CompanyId=@CompanyId, CategoryId=@CategoryId, Code=@Code, Name=@Name, Description=@Description, PurchaseDate=@PurchaseDate, PurchasePrice=@PurchasePrice, CurrentValue=@CurrentValue, Location=@Location, AssignedTo=@AssignedTo, PersonnelId=@PersonnelId, Status=@Status, WarrantyEndDate=@WarrantyEndDate, DepreciationRate=@DepreciationRate, ImageUrl=@ImageUrl, SerialNo=@SerialNo, Notes=@Notes, IsActive=@IsActive, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- ========== مالی (FinanceAccounts) ==========
IF OBJECT_ID('koch.spFinanceAccounts_List', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_List;
GO
CREATE PROCEDURE koch.spFinanceAccounts_List @StoreId INT = NULL, @CompanyId INT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, ParentId, Code, Name, Type, Balance, Currency, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.FinanceAccounts WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@CompanyId IS NULL OR CompanyId = @CompanyId) ORDER BY SortOrder, Code;
END
GO

IF OBJECT_ID('koch.spFinanceAccounts_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_GetById;
GO
CREATE PROCEDURE koch.spFinanceAccounts_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, ParentId, Code, Name, Type, Balance, Currency, IsActive, SortOrder, CreatedAt, UpdatedAt FROM koch.FinanceAccounts WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spFinanceAccounts_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_Insert;
GO
CREATE PROCEDURE koch.spFinanceAccounts_Insert
    @StoreId INT = NULL, @CompanyId INT = NULL, @ParentId INT = NULL, @Code NVARCHAR(32), @Name NVARCHAR(256), @Type NVARCHAR(32) = NULL, @Balance DECIMAL(18,2) = 0, @Currency NVARCHAR(8) = NULL, @IsActive BIT = 1, @SortOrder INT = 0, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.FinanceAccounts (StoreId, CompanyId, ParentId, Code, Name, Type, Balance, Currency, IsActive, SortOrder, CreatedAt)
    VALUES (@StoreId, @CompanyId, @ParentId, @Code, @Name, @Type, @Balance, @Currency, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spFinanceAccounts_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spFinanceAccounts_Update;
GO
CREATE PROCEDURE koch.spFinanceAccounts_Update
    @Id INT, @StoreId INT = NULL, @CompanyId INT = NULL, @ParentId INT = NULL, @Code NVARCHAR(32), @Name NVARCHAR(256), @Type NVARCHAR(32) = NULL, @Balance DECIMAL(18,2) = 0, @Currency NVARCHAR(8) = NULL, @IsActive BIT = 1, @SortOrder INT = 0, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.FinanceAccounts SET StoreId=@StoreId, CompanyId=@CompanyId, ParentId=@ParentId, Code=@Code, Name=@Name, Type=@Type, Balance=@Balance, Currency=@Currency, IsActive=@IsActive, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- ========== CRM (CrmContacts) ==========
IF OBJECT_ID('koch.spCrmContacts_List', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_List;
GO
CREATE PROCEDURE koch.spCrmContacts_List @StoreId INT = NULL, @CompanyId INT = NULL, @IsActive BIT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, FullName, Phone, Phone2, Email, Company, JobTitle, Address, City, Province, PostalCode, Source, Tags, Notes, AssignedTo, Status, Rating, IsActive, CreatedBy, CreatedAt, UpdatedAt, LastActivityAt
    FROM koch.CrmContacts WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@CompanyId IS NULL OR CompanyId = @CompanyId) AND (@IsActive IS NULL OR IsActive = @IsActive) ORDER BY FullName;
END
GO

IF OBJECT_ID('koch.spCrmContacts_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_GetById;
GO
CREATE PROCEDURE koch.spCrmContacts_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, FullName, Phone, Phone2, Email, Company, JobTitle, Address, City, Province, PostalCode, Source, Tags, Notes, AssignedTo, Status, Rating, IsActive, CreatedBy, CreatedAt, UpdatedAt, LastActivityAt FROM koch.CrmContacts WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spCrmContacts_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_Insert;
GO
CREATE PROCEDURE koch.spCrmContacts_Insert
    @StoreId INT=NULL, @CompanyId INT=NULL, @FullName NVARCHAR(128), @Phone NVARCHAR(20)=NULL, @Phone2 NVARCHAR(20)=NULL, @Email NVARCHAR(256)=NULL, @Company NVARCHAR(256)=NULL, @JobTitle NVARCHAR(128)=NULL, @Address NVARCHAR(512)=NULL, @City NVARCHAR(64)=NULL, @Province NVARCHAR(64)=NULL, @PostalCode NVARCHAR(20)=NULL, @Source NVARCHAR(64)=NULL, @Tags NVARCHAR(512)=NULL, @Notes NVARCHAR(MAX)=NULL, @AssignedTo INT=NULL, @Status NVARCHAR(32)=NULL, @Rating TINYINT=NULL, @IsActive BIT=1, @CreatedBy INT=NULL, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.CrmContacts (StoreId, CompanyId, FullName, Phone, Phone2, Email, Company, JobTitle, Address, City, Province, PostalCode, Source, Tags, Notes, AssignedTo, Status, Rating, IsActive, CreatedBy, CreatedAt)
    VALUES (@StoreId, @CompanyId, @FullName, @Phone, @Phone2, @Email, @Company, @JobTitle, @Address, @City, @Province, @PostalCode, @Source, @Tags, @Notes, @AssignedTo, @Status, @Rating, @IsActive, @CreatedBy, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spCrmContacts_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_Update;
GO
CREATE PROCEDURE koch.spCrmContacts_Update
    @Id INT, @StoreId INT=NULL, @CompanyId INT=NULL, @FullName NVARCHAR(128), @Phone NVARCHAR(20)=NULL, @Phone2 NVARCHAR(20)=NULL, @Email NVARCHAR(256)=NULL, @Company NVARCHAR(256)=NULL, @JobTitle NVARCHAR(128)=NULL, @Address NVARCHAR(512)=NULL, @City NVARCHAR(64)=NULL, @Province NVARCHAR(64)=NULL, @PostalCode NVARCHAR(20)=NULL, @Source NVARCHAR(64)=NULL, @Tags NVARCHAR(512)=NULL, @Notes NVARCHAR(MAX)=NULL, @AssignedTo INT=NULL, @Status NVARCHAR(32)=NULL, @Rating TINYINT=NULL, @IsActive BIT=1, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.CrmContacts SET StoreId=@StoreId, CompanyId=@CompanyId, FullName=@FullName, Phone=@Phone, Phone2=@Phone2, Email=@Email, Company=@Company, JobTitle=@JobTitle, Address=@Address, City=@City, Province=@Province, PostalCode=@PostalCode, Source=@Source, Tags=@Tags, Notes=@Notes, AssignedTo=@AssignedTo, Status=@Status, Rating=@Rating, IsActive=@IsActive, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- ========== قرارداد (Contracts) ==========
IF OBJECT_ID('koch.spContracts_List', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_List;
GO
CREATE PROCEDURE koch.spContracts_List @StoreId INT = NULL, @CompanyId INT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, ContractNo, Title, PartyName, PartyContact, PartyPhone, StartDate, EndDate, Value, Currency, Status, Type, DocumentUrl, Notes, ReminderDays, ReminderSentAt, RenewalCount, ParentContractId, CreatedBy, CreatedAt, UpdatedAt
    FROM koch.Contracts WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@CompanyId IS NULL OR CompanyId = @CompanyId) ORDER BY CreatedAt DESC;
END
GO

IF OBJECT_ID('koch.spContracts_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_GetById;
GO
CREATE PROCEDURE koch.spContracts_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, ContractNo, Title, PartyName, PartyContact, PartyPhone, StartDate, EndDate, Value, Currency, Status, Type, DocumentUrl, Notes, ReminderDays, ReminderSentAt, RenewalCount, ParentContractId, CreatedBy, CreatedAt, UpdatedAt FROM koch.Contracts WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spContracts_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_Insert;
GO
CREATE PROCEDURE koch.spContracts_Insert
    @StoreId INT = NULL, @CompanyId INT = NULL, @ContractNo NVARCHAR(64) = NULL, @Title NVARCHAR(256), @PartyName NVARCHAR(256) = NULL, @PartyContact NVARCHAR(256) = NULL, @PartyPhone NVARCHAR(20) = NULL,
    @StartDate DATE = NULL, @EndDate DATE = NULL, @Value DECIMAL(18,2) = NULL, @Currency NVARCHAR(8) = NULL, @Status NVARCHAR(32) = NULL, @Type NVARCHAR(64) = NULL, @DocumentUrl NVARCHAR(1024) = NULL, @Notes NVARCHAR(MAX) = NULL, @ReminderDays INT = NULL, @ParentContractId INT = NULL, @CreatedBy INT = NULL, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.Contracts (StoreId, CompanyId, ContractNo, Title, PartyName, PartyContact, PartyPhone, StartDate, EndDate, Value, Currency, Status, Type, DocumentUrl, Notes, ReminderDays, ParentContractId, CreatedBy, CreatedAt)
    VALUES (@StoreId, @CompanyId, @ContractNo, @Title, @PartyName, @PartyContact, @PartyPhone, @StartDate, @EndDate, @Value, @Currency, @Status, @Type, @DocumentUrl, @Notes, @ReminderDays, @ParentContractId, @CreatedBy, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spContracts_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_Update;
GO
CREATE PROCEDURE koch.spContracts_Update
    @Id INT, @StoreId INT = NULL, @CompanyId INT = NULL, @ContractNo NVARCHAR(64) = NULL, @Title NVARCHAR(256), @PartyName NVARCHAR(256) = NULL, @PartyContact NVARCHAR(256) = NULL, @PartyPhone NVARCHAR(20) = NULL,
    @StartDate DATE = NULL, @EndDate DATE = NULL, @Value DECIMAL(18,2) = NULL, @Currency NVARCHAR(8) = NULL, @Status NVARCHAR(32) = NULL, @Type NVARCHAR(64) = NULL, @DocumentUrl NVARCHAR(1024) = NULL, @Notes NVARCHAR(MAX) = NULL, @ReminderDays INT = NULL, @ParentContractId INT = NULL, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.Contracts SET StoreId=@StoreId, CompanyId=@CompanyId, ContractNo=@ContractNo, Title=@Title, PartyName=@PartyName, PartyContact=@PartyContact, PartyPhone=@PartyPhone, StartDate=@StartDate, EndDate=@EndDate, Value=@Value, Currency=@Currency, Status=@Status, Type=@Type, DocumentUrl=@DocumentUrl, Notes=@Notes, ReminderDays=@ReminderDays, ParentContractId=@ParentContractId, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- ========== پروژه (Projects) ==========
IF OBJECT_ID('koch.spProjects_List', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_List;
GO
CREATE PROCEDURE koch.spProjects_List @StoreId INT = NULL, @CompanyId INT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, Title, Description, Status, Priority, StartDate, EndDate, Progress, CreatedBy, CreatedAt, UpdatedAt
    FROM koch.Projects WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@CompanyId IS NULL OR CompanyId = @CompanyId) ORDER BY CreatedAt DESC;
END
GO

IF OBJECT_ID('koch.spProjects_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_GetById;
GO
CREATE PROCEDURE koch.spProjects_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, Title, Description, Status, Priority, StartDate, EndDate, Progress, CreatedBy, CreatedAt, UpdatedAt FROM koch.Projects WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spProjects_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_Insert;
GO
CREATE PROCEDURE koch.spProjects_Insert
    @StoreId INT = NULL, @CompanyId INT = NULL, @Title NVARCHAR(256), @Description NVARCHAR(MAX) = NULL, @Status NVARCHAR(32) = N'Planning', @Priority NVARCHAR(32) = NULL, @StartDate DATE = NULL, @EndDate DATE = NULL, @Progress TINYINT = 0, @CreatedBy INT = NULL, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.Projects (StoreId, CompanyId, Title, Description, Status, Priority, StartDate, EndDate, Progress, CreatedBy, CreatedAt)
    VALUES (@StoreId, @CompanyId, @Title, @Description, @Status, @Priority, @StartDate, @EndDate, @Progress, @CreatedBy, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spProjects_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spProjects_Update;
GO
CREATE PROCEDURE koch.spProjects_Update
    @Id INT, @StoreId INT = NULL, @CompanyId INT = NULL, @Title NVARCHAR(256), @Description NVARCHAR(MAX) = NULL, @Status NVARCHAR(32), @Priority NVARCHAR(32) = NULL, @StartDate DATE = NULL, @EndDate DATE = NULL, @Progress TINYINT = 0, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.Projects SET StoreId=@StoreId, CompanyId=@CompanyId, Title=@Title, Description=@Description, Status=@Status, Priority=@Priority, StartDate=@StartDate, EndDate=@EndDate, Progress=@Progress, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- ========== محصول (Products) ==========
IF OBJECT_ID('koch.spProducts_List', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_List;
GO
CREATE PROCEDURE koch.spProducts_List @StoreId INT = NULL, @CompanyId INT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, CategoryId, Name, Slug, Description, ShortDescription, Price, CompareAtPrice, Cost, Sku, Barcode, Quantity, IsActive, IsFeatured, ImageUrl, SortOrder, CreatedAt, UpdatedAt
    FROM koch.Products WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@CompanyId IS NULL OR CompanyId = @CompanyId) ORDER BY SortOrder, Name;
END
GO

IF OBJECT_ID('koch.spProducts_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_GetById;
GO
CREATE PROCEDURE koch.spProducts_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, CategoryId, Name, Slug, Description, ShortDescription, Price, CompareAtPrice, Cost, Sku, Barcode, Quantity, IsActive, IsFeatured, ImageUrl, SortOrder, CreatedAt, UpdatedAt FROM koch.Products WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spProducts_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_Insert;
GO
CREATE PROCEDURE koch.spProducts_Insert
    @StoreId INT = NULL, @CompanyId INT = NULL, @CategoryId INT = NULL, @Name NVARCHAR(256), @Slug NVARCHAR(300) = NULL, @Description NVARCHAR(MAX) = NULL, @ShortDescription NVARCHAR(512) = NULL,
    @Price DECIMAL(18,2) = NULL, @CompareAtPrice DECIMAL(18,2) = NULL, @Cost DECIMAL(18,2) = NULL, @Sku NVARCHAR(64) = NULL, @Barcode NVARCHAR(64) = NULL, @Quantity INT = 0, @IsActive BIT = 1, @IsFeatured BIT = 0, @ImageUrl NVARCHAR(1024) = NULL, @SortOrder INT = 0, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.Products (StoreId, CompanyId, CategoryId, Name, Slug, Description, ShortDescription, Price, CompareAtPrice, Cost, Sku, Barcode, Quantity, IsActive, IsFeatured, ImageUrl, SortOrder, CreatedAt)
    VALUES (@StoreId, @CompanyId, @CategoryId, @Name, @Slug, @Description, @ShortDescription, @Price, @CompareAtPrice, @Cost, @Sku, @Barcode, @Quantity, @IsActive, @IsFeatured, @ImageUrl, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spProducts_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spProducts_Update;
GO
CREATE PROCEDURE koch.spProducts_Update
    @Id INT, @StoreId INT = NULL, @CompanyId INT = NULL, @CategoryId INT = NULL, @Name NVARCHAR(256), @Slug NVARCHAR(300) = NULL, @Description NVARCHAR(MAX) = NULL, @ShortDescription NVARCHAR(512) = NULL,
    @Price DECIMAL(18,2) = NULL, @CompareAtPrice DECIMAL(18,2) = NULL, @Cost DECIMAL(18,2) = NULL, @Sku NVARCHAR(64) = NULL, @Barcode NVARCHAR(64) = NULL, @Quantity INT = 0, @IsActive BIT = 1, @IsFeatured BIT = 0, @ImageUrl NVARCHAR(1024) = NULL, @SortOrder INT = 0, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.Products SET StoreId=@StoreId, CompanyId=@CompanyId, CategoryId=@CategoryId, Name=@Name, Slug=@Slug, Description=@Description, ShortDescription=@ShortDescription, Price=@Price, CompareAtPrice=@CompareAtPrice, Cost=@Cost, Sku=@Sku, Barcode=@Barcode, Quantity=@Quantity, IsActive=@IsActive, IsFeatured=@IsFeatured, ImageUrl=@ImageUrl, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- ========== آموزش (TrainingCourses) ==========
IF OBJECT_ID('koch.spTrainingCourses_List', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_List;
GO
CREATE PROCEDURE koch.spTrainingCourses_List @StoreId INT = NULL, @CompanyId INT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, Code, Title, Description, DurationHours, StartDate, EndDate, MaxParticipants, Location, InstructorName, ImageUrl, IsActive, SortOrder, CreatedAt, UpdatedAt
    FROM koch.TrainingCourses WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@CompanyId IS NULL OR CompanyId = @CompanyId) ORDER BY SortOrder, Title;
END
GO

IF OBJECT_ID('koch.spTrainingCourses_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_GetById;
GO
CREATE PROCEDURE koch.spTrainingCourses_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, CompanyId, StoreId, Code, Title, Description, DurationHours, StartDate, EndDate, MaxParticipants, Location, InstructorName, ImageUrl, IsActive, SortOrder, CreatedAt, UpdatedAt FROM koch.TrainingCourses WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spTrainingCourses_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_Insert;
GO
CREATE PROCEDURE koch.spTrainingCourses_Insert
    @StoreId INT = NULL, @CompanyId INT = NULL, @Code NVARCHAR(32) = NULL, @Title NVARCHAR(256), @Description NVARCHAR(MAX) = NULL, @DurationHours DECIMAL(6,2) = NULL, @StartDate DATE = NULL, @EndDate DATE = NULL, @MaxParticipants INT = NULL, @Location NVARCHAR(256) = NULL, @InstructorName NVARCHAR(128) = NULL, @ImageUrl NVARCHAR(1024) = NULL, @IsActive BIT = 1, @SortOrder INT = 0, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.TrainingCourses (StoreId, CompanyId, Code, Title, Description, DurationHours, StartDate, EndDate, MaxParticipants, Location, InstructorName, ImageUrl, IsActive, SortOrder, CreatedAt)
    VALUES (@StoreId, @CompanyId, @Code, @Title, @Description, @DurationHours, @StartDate, @EndDate, @MaxParticipants, @Location, @InstructorName, @ImageUrl, @IsActive, @SortOrder, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spTrainingCourses_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spTrainingCourses_Update;
GO
CREATE PROCEDURE koch.spTrainingCourses_Update
    @Id INT, @StoreId INT = NULL, @CompanyId INT = NULL, @Code NVARCHAR(32) = NULL, @Title NVARCHAR(256), @Description NVARCHAR(MAX) = NULL, @DurationHours DECIMAL(6,2) = NULL, @StartDate DATE = NULL, @EndDate DATE = NULL, @MaxParticipants INT = NULL, @Location NVARCHAR(256) = NULL, @InstructorName NVARCHAR(128) = NULL, @ImageUrl NVARCHAR(1024) = NULL, @IsActive BIT = 1, @SortOrder INT = 0, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.TrainingCourses SET StoreId=@StoreId, CompanyId=@CompanyId, Code=@Code, Title=@Title, Description=@Description, DurationHours=@DurationHours, StartDate=@StartDate, EndDate=@EndDate, MaxParticipants=@MaxParticipants, Location=@Location, InstructorName=@InstructorName, ImageUrl=@ImageUrl, IsActive=@IsActive, SortOrder=@SortOrder, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

PRINT 'فاز ۴: CompanyId به SPهای دارایی، مالی، CRM، قرارداد، پروژه، محصول، آموزش اضافه شد.';
GO
