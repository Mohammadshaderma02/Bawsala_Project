IF OBJECT_ID(N'dbo.NotificationOutbox', N'U') IS NULL
    THROW 50013, 'NotificationOutbox must exist before applying migration 013.', 1;

IF COL_LENGTH(N'dbo.NotificationOutbox', N'ClaimedAtUtc') IS NULL
    ALTER TABLE dbo.NotificationOutbox ADD ClaimedAtUtc DATETIME2 NULL;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.NotificationOutbox')
      AND name = N'IX_NotificationOutbox_Channel_Status_Created'
)
    CREATE INDEX IX_NotificationOutbox_Channel_Status_Created
        ON dbo.NotificationOutbox(Channel, Status, CreatedAtUtc, Id)
        INCLUDE (Attempts, UserId, TemplateKey);
