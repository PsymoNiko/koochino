-- Stored procedures ماژول قراردادها (CRUD)
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- List
IF OBJECT_ID('koch.spContracts_List', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_List;
GO
CREATE PROCEDURE koch.spContracts_List
    @StoreId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, ContractNo, Title, PartyName, PartyContact, PartyPhone,
           StartDate, EndDate, Value, Currency, Status, Type, DocumentUrl, Notes,
           ReminderDays, ReminderSentAt, RenewalCount, ParentContractId, CreatedBy, CreatedAt, UpdatedAt
    FROM koch.Contracts
    WHERE (@StoreId IS NULL OR StoreId = @StoreId)
    ORDER BY CreatedAt DESC;
END
GO

-- GetById
IF OBJECT_ID('koch.spContracts_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_GetById;
GO
CREATE PROCEDURE koch.spContracts_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, StoreId, ContractNo, Title, PartyName, PartyContact, PartyPhone,
           StartDate, EndDate, Value, Currency, Status, Type, DocumentUrl, Notes,
           ReminderDays, ReminderSentAt, RenewalCount, ParentContractId, CreatedBy, CreatedAt, UpdatedAt
    FROM koch.Contracts
    WHERE Id = @Id;
END
GO

-- Insert
IF OBJECT_ID('koch.spContracts_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_Insert;
GO
CREATE PROCEDURE koch.spContracts_Insert
    @StoreId INT = NULL,
    @ContractNo NVARCHAR(64) = NULL,
    @Title NVARCHAR(256),
    @PartyName NVARCHAR(256) = NULL,
    @PartyContact NVARCHAR(256) = NULL,
    @PartyPhone NVARCHAR(20) = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @Value DECIMAL(18,2) = NULL,
    @Currency NVARCHAR(8) = NULL,
    @Status NVARCHAR(32) = NULL,
    @Type NVARCHAR(64) = NULL,
    @DocumentUrl NVARCHAR(1024) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
    @ReminderDays INT = NULL,
    @ParentContractId INT = NULL,
    @CreatedBy INT = NULL,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.Contracts (StoreId, ContractNo, Title, PartyName, PartyContact, PartyPhone,
        StartDate, EndDate, Value, Currency, Status, Type, DocumentUrl, Notes, ReminderDays, ParentContractId, CreatedBy, CreatedAt)
    VALUES (@StoreId, @ContractNo, @Title, @PartyName, @PartyContact, @PartyPhone,
        @StartDate, @EndDate, @Value, @Currency, @Status, @Type, @DocumentUrl, @Notes, @ReminderDays, @ParentContractId, @CreatedBy, SYSDATETIME());
    SET @Id = SCOPE_IDENTITY();
END
GO

-- Update
IF OBJECT_ID('koch.spContracts_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_Update;
GO
CREATE PROCEDURE koch.spContracts_Update
    @Id INT,
    @StoreId INT = NULL,
    @ContractNo NVARCHAR(64) = NULL,
    @Title NVARCHAR(256),
    @PartyName NVARCHAR(256) = NULL,
    @PartyContact NVARCHAR(256) = NULL,
    @PartyPhone NVARCHAR(20) = NULL,
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @Value DECIMAL(18,2) = NULL,
    @Currency NVARCHAR(8) = NULL,
    @Status NVARCHAR(32) = NULL,
    @Type NVARCHAR(64) = NULL,
    @DocumentUrl NVARCHAR(1024) = NULL,
    @Notes NVARCHAR(MAX) = NULL,
    @ReminderDays INT = NULL,
    @ParentContractId INT = NULL,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.Contracts SET
        StoreId = @StoreId, ContractNo = @ContractNo, Title = @Title,
        PartyName = @PartyName, PartyContact = @PartyContact, PartyPhone = @PartyPhone,
        StartDate = @StartDate, EndDate = @EndDate, Value = @Value, Currency = @Currency,
        Status = @Status, Type = @Type, DocumentUrl = @DocumentUrl, Notes = @Notes,
        ReminderDays = @ReminderDays, ParentContractId = @ParentContractId, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- Delete
IF OBJECT_ID('koch.spContracts_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spContracts_Delete;
GO
CREATE PROCEDURE koch.spContracts_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.Contracts WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
