-- جدول تنظیمات (کلید-مقدار) برای ماژول‌ها و سایر تنظیمات
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Settings')
BEGIN
    CREATE TABLE [dbo].[Settings] (
        [Key]   NVARCHAR(128) NOT NULL PRIMARY KEY,
        [Value] NVARCHAR(512) NULL
    );
END
GO

-- مقادیر پیش‌فرض ماژول‌ها (همه فعال). در صورت وجود قبلی، به‌روز نمی‌شود.
MERGE [dbo].[Settings] AS t
USING (VALUES
    (N'Modules:Users', N'true'),
    (N'Modules:Stores', N'true'),
    (N'Modules:Blog', N'true'),
    (N'Modules:Shop', N'true'),
    (N'Modules:Points', N'true'),
    (N'Modules:Announcements', N'true'),
    (N'Modules:Common', N'true'),
    (N'Modules:Products', N'true'),
    (N'Modules:Personnel', N'true'),
    (N'Modules:ProjectManagement', N'true'),
    (N'Modules:Inventory', N'true'),
    (N'Modules:CRM', N'true'),
    (N'Modules:Tickets', N'true'),
    (N'Modules:Reports', N'true'),
    (N'Modules:Finance', N'true'),
    (N'Modules:Contracts', N'true'),
    (N'Modules:Assets', N'true'),
    (N'Modules:Training', N'true')
) AS s ([Key], [Value])
ON t.[Key] = s.[Key]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Key], [Value]) VALUES (s.[Key], s.[Value]);
GO
