IF OBJECT_ID(N'dbo.ClientAccounts', N'U') IS NULL
    THROW 50014, 'Run the ClientAccounts migrations before migration 014.', 1;

IF COL_LENGTH(N'dbo.ClientAccounts', N'IsDeleted') IS NULL
    ALTER TABLE dbo.ClientAccounts ADD IsDeleted BIT NOT NULL CONSTRAINT DF_ClientAccounts_IsDeleted DEFAULT 0;

IF COL_LENGTH(N'dbo.ClientAccounts', N'DeletedAtUtc') IS NULL
    ALTER TABLE dbo.ClientAccounts ADD DeletedAtUtc DATETIME2 NULL;

GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ClientAccounts')
      AND name = N'CK_ClientAccounts_DeletionState'
)
    ALTER TABLE dbo.ClientAccounts ADD CONSTRAINT CK_ClientAccounts_DeletionState
        CHECK ((IsDeleted = 0 AND DeletedAtUtc IS NULL) OR (IsDeleted = 1 AND DeletedAtUtc IS NOT NULL));

IF OBJECT_ID(N'dbo.AdminUserDeletionLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminUserDeletionLogs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdminUserDeletionLogs PRIMARY KEY,
        ActorUserId UNIQUEIDENTIFIER NOT NULL,
        ActorName NVARCHAR(160) NOT NULL,
        ActorEmail NVARCHAR(320) NOT NULL,
        TargetUserId UNIQUEIDENTIFIER NOT NULL,
        TargetName NVARCHAR(160) NOT NULL,
        TargetEmail NVARCHAR(320) NOT NULL,
        TargetRole NVARCHAR(20) NOT NULL,
        DeletedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_AdminUserDeletionLogs_DeletedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;

GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AdminUserDeletionLogs')
      AND name = N'IX_AdminUserDeletionLogs_DeletedAtUtc'
)
BEGIN
    CREATE INDEX IX_AdminUserDeletionLogs_DeletedAtUtc
        ON dbo.AdminUserDeletionLogs(DeletedAtUtc DESC, Id DESC);
END;
