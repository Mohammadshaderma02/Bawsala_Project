IF OBJECT_ID(N'dbo.ClientAccounts', N'U') IS NULL
    THROW 50001, 'Run migration 005 before migration 008.', 1;

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        TokenHash CHAR(64) NOT NULL,
        ExpiresAtUtc DATETIME2 NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PasswordResetTokens_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UsedAtUtc DATETIME2 NULL,
        CONSTRAINT FK_PasswordResetTokens_User FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id),
        CONSTRAINT UQ_PasswordResetTokens_TokenHash UNIQUE (TokenHash)
    );

    CREATE INDEX IX_PasswordResetTokens_User_Created
        ON dbo.PasswordResetTokens(UserId, CreatedAtUtc DESC);
    CREATE INDEX IX_PasswordResetTokens_Expiry
        ON dbo.PasswordResetTokens(ExpiresAtUtc)
        WHERE UsedAtUtc IS NULL;
END;
