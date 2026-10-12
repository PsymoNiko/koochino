-- SPهای مخاطبین CRM - CRUD
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF OBJECT_ID('koch.spCrmContacts_List', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_List;
GO
CREATE PROCEDURE koch.spCrmContacts_List @StoreId INT = NULL, @IsActive BIT = NULL
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, StoreId, FullName, Phone, Phone2, Email, Company, JobTitle, Address, City, Province, PostalCode, Source, Tags, Notes, AssignedTo, Status, Rating, IsActive, CreatedBy, CreatedAt, UpdatedAt, LastActivityAt
    FROM koch.CrmContacts WHERE (@StoreId IS NULL OR StoreId = @StoreId) AND (@IsActive IS NULL OR IsActive = @IsActive) ORDER BY FullName;
END
GO

IF OBJECT_ID('koch.spCrmContacts_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_GetById;
GO
CREATE PROCEDURE koch.spCrmContacts_GetById @Id INT
AS
BEGIN SET NOCOUNT ON;
    SELECT Id, StoreId, FullName, Phone, Phone2, Email, Company, JobTitle, Address, City, Province, PostalCode, Source, Tags, Notes, AssignedTo, Status, Rating, IsActive, CreatedBy, CreatedAt, UpdatedAt, LastActivityAt FROM koch.CrmContacts WHERE Id = @Id;
END
GO

IF OBJECT_ID('koch.spCrmContacts_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_Insert;
GO
CREATE PROCEDURE koch.spCrmContacts_Insert
    @StoreId INT=NULL, @FullName NVARCHAR(128), @Phone NVARCHAR(20)=NULL, @Phone2 NVARCHAR(20)=NULL, @Email NVARCHAR(256)=NULL, @Company NVARCHAR(256)=NULL, @JobTitle NVARCHAR(128)=NULL, @Address NVARCHAR(512)=NULL, @City NVARCHAR(64)=NULL, @Province NVARCHAR(64)=NULL, @PostalCode NVARCHAR(20)=NULL, @Source NVARCHAR(64)=NULL, @Tags NVARCHAR(512)=NULL, @Notes NVARCHAR(MAX)=NULL, @AssignedTo INT=NULL, @Status NVARCHAR(32)=NULL, @Rating TINYINT=NULL, @IsActive BIT=1, @CreatedBy INT=NULL, @Id INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    INSERT INTO koch.CrmContacts (StoreId, FullName, Phone, Phone2, Email, Company, JobTitle, Address, City, Province, PostalCode, Source, Tags, Notes, AssignedTo, Status, Rating, IsActive, CreatedBy, CreatedAt)
    VALUES (@StoreId, @FullName, @Phone, @Phone2, @Email, @Company, @JobTitle, @Address, @City, @Province, @PostalCode, @Source, @Tags, @Notes, @AssignedTo, @Status, @Rating, @IsActive, @CreatedBy, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('koch.spCrmContacts_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_Update;
GO
CREATE PROCEDURE koch.spCrmContacts_Update
    @Id INT, @StoreId INT=NULL, @FullName NVARCHAR(128), @Phone NVARCHAR(20)=NULL, @Phone2 NVARCHAR(20)=NULL, @Email NVARCHAR(256)=NULL, @Company NVARCHAR(256)=NULL, @JobTitle NVARCHAR(128)=NULL, @Address NVARCHAR(512)=NULL, @City NVARCHAR(64)=NULL, @Province NVARCHAR(64)=NULL, @PostalCode NVARCHAR(20)=NULL, @Source NVARCHAR(64)=NULL, @Tags NVARCHAR(512)=NULL, @Notes NVARCHAR(MAX)=NULL, @AssignedTo INT=NULL, @Status NVARCHAR(32)=NULL, @Rating TINYINT=NULL, @IsActive BIT=1, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON;
    UPDATE koch.CrmContacts SET StoreId=@StoreId, FullName=@FullName, Phone=@Phone, Phone2=@Phone2, Email=@Email, Company=@Company, JobTitle=@JobTitle, Address=@Address, City=@City, Province=@Province, PostalCode=@PostalCode, Source=@Source, Tags=@Tags, Notes=@Notes, AssignedTo=@AssignedTo, Status=@Status, Rating=@Rating, IsActive=@IsActive, UpdatedAt=SYSDATETIME() WHERE Id=@Id;
    SET @Result = @@ROWCOUNT;
END
GO

IF OBJECT_ID('koch.spCrmContacts_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spCrmContacts_Delete;
GO
CREATE PROCEDURE koch.spCrmContacts_Delete @Id INT, @Result INT OUTPUT
AS
BEGIN SET NOCOUNT ON; DELETE FROM koch.CrmContacts WHERE Id = @Id; SET @Result = @@ROWCOUNT; END
GO
