-- Stored procedures ماژول تیکت (CRUD تیکت + لیست دپارتمان‌ها)
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

-- لیست دپارتمان‌ها (برای دراپ‌داون)
IF OBJECT_ID('koch.spTicketDepartments_List', 'P') IS NOT NULL DROP PROCEDURE koch.spTicketDepartments_List;
GO
CREATE PROCEDURE koch.spTicketDepartments_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name FROM koch.TicketDepartments WHERE IsActive = 1 ORDER BY SortOrder, Name;
END
GO

-- Tickets: List
IF OBJECT_ID('koch.spTickets_List', 'P') IS NOT NULL DROP PROCEDURE koch.spTickets_List;
GO
CREATE PROCEDURE koch.spTickets_List
    @DepartmentId INT = NULL,
    @Status NVARCHAR(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, DepartmentId, TicketNo, UserId, ContactName, ContactEmail, ContactPhone,
           Subject, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, ClosedAt, ClosedBy, Resolution
    FROM koch.Tickets
    WHERE (@DepartmentId IS NULL OR DepartmentId = @DepartmentId)
      AND (@Status IS NULL OR Status = @Status)
    ORDER BY CreatedAt DESC;
END
GO

-- Tickets: GetById
IF OBJECT_ID('koch.spTickets_GetById', 'P') IS NOT NULL DROP PROCEDURE koch.spTickets_GetById;
GO
CREATE PROCEDURE koch.spTickets_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, DepartmentId, TicketNo, UserId, ContactName, ContactEmail, ContactPhone,
           Subject, Status, Priority, AssignedTo, CreatedAt, UpdatedAt, ClosedAt, ClosedBy, Resolution
    FROM koch.Tickets WHERE Id = @Id;
END
GO

-- Tickets: Insert
IF OBJECT_ID('koch.spTickets_Insert', 'P') IS NOT NULL DROP PROCEDURE koch.spTickets_Insert;
GO
CREATE PROCEDURE koch.spTickets_Insert
    @DepartmentId INT = NULL,
    @TicketNo NVARCHAR(32) = NULL,
    @UserId INT = NULL,
    @ContactName NVARCHAR(128) = NULL,
    @ContactEmail NVARCHAR(256) = NULL,
    @ContactPhone NVARCHAR(20) = NULL,
    @Subject NVARCHAR(256),
    @Status NVARCHAR(32) = N'Open',
    @Priority NVARCHAR(32) = NULL,
    @AssignedTo INT = NULL,
    @Resolution NVARCHAR(MAX) = NULL,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO koch.Tickets (DepartmentId, TicketNo, UserId, ContactName, ContactEmail, ContactPhone,
        Subject, Status, Priority, AssignedTo, CreatedAt, Resolution)
    VALUES (@DepartmentId, @TicketNo, @UserId, @ContactName, @ContactEmail, @ContactPhone,
        @Subject, @Status, @Priority, @AssignedTo, SYSDATETIME(), @Resolution);
    SET @Id = SCOPE_IDENTITY();
END
GO

-- Tickets: Update
IF OBJECT_ID('koch.spTickets_Update', 'P') IS NOT NULL DROP PROCEDURE koch.spTickets_Update;
GO
CREATE PROCEDURE koch.spTickets_Update
    @Id INT,
    @DepartmentId INT = NULL,
    @TicketNo NVARCHAR(32) = NULL,
    @UserId INT = NULL,
    @ContactName NVARCHAR(128) = NULL,
    @ContactEmail NVARCHAR(256) = NULL,
    @ContactPhone NVARCHAR(20) = NULL,
    @Subject NVARCHAR(256),
    @Status NVARCHAR(32),
    @Priority NVARCHAR(32) = NULL,
    @AssignedTo INT = NULL,
    @Resolution NVARCHAR(MAX) = NULL,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE koch.Tickets SET
        DepartmentId = @DepartmentId, TicketNo = @TicketNo, UserId = @UserId,
        ContactName = @ContactName, ContactEmail = @ContactEmail, ContactPhone = @ContactPhone,
        Subject = @Subject, Status = @Status, Priority = @Priority, AssignedTo = @AssignedTo,
        Resolution = @Resolution, UpdatedAt = SYSDATETIME()
    WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO

-- Tickets: Delete
IF OBJECT_ID('koch.spTickets_Delete', 'P') IS NOT NULL DROP PROCEDURE koch.spTickets_Delete;
GO
CREATE PROCEDURE koch.spTickets_Delete
    @Id INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM koch.Tickets WHERE Id = @Id;
    SET @Result = @@ROWCOUNT;
END
GO
