-- ماژول قراردادها: قرارداد، طرف قرارداد، تاریخ انقضا، اعلان
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'koch') EXEC('CREATE SCHEMA koch');
GO

IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'koch' AND t.name = 'Contracts')
BEGIN
    CREATE TABLE [koch].[Contracts] (
        [Id]            INT IDENTITY(1,1) NOT NULL,
        [StoreId]       INT NULL,
        [ContractNo]    NVARCHAR(64) NULL,
        [Title]         NVARCHAR(256) NOT NULL,
        [PartyName]     NVARCHAR(256) NULL,
        [PartyContact]  NVARCHAR(256) NULL,
        [PartyPhone]    NVARCHAR(20) NULL,
        [StartDate]     DATE NULL,
        [EndDate]       DATE NULL,
        [Value]         DECIMAL(18,2) NULL,
        [Currency]      NVARCHAR(8) NULL,
        [Status]        NVARCHAR(32) NULL,
        [Type]          NVARCHAR(64) NULL,
        [DocumentUrl]   NVARCHAR(1024) NULL,
        [Notes]         NVARCHAR(MAX) NULL,
        [ReminderDays]  INT NULL,
        [ReminderSentAt] DATETIME2(2) NULL,
        [RenewalCount]  INT NOT NULL DEFAULT 0,
        [ParentContractId] INT NULL,
        [CreatedBy]     INT NULL,
        [CreatedAt]     DATETIME2(2) NOT NULL DEFAULT SYSDATETIME(),
        [UpdatedAt]     DATETIME2(2) NULL,
        CONSTRAINT [PK_Contracts] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Contracts_StoreId] ON [koch].[Contracts] ([StoreId]);
    CREATE NONCLUSTERED INDEX [IX_Contracts_EndDate] ON [koch].[Contracts] ([EndDate]);
    CREATE NONCLUSTERED INDEX [IX_Contracts_Status] ON [koch].[Contracts] ([Status]);
END
GO
