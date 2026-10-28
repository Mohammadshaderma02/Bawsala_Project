IF COL_LENGTH(N'dbo.ClientAccounts', N'IsEmailVerified') IS NULL
    ALTER TABLE dbo.ClientAccounts ADD IsEmailVerified BIT NOT NULL CONSTRAINT DF_ClientAccounts_IsEmailVerified DEFAULT 1;

IF OBJECT_ID(N'dbo.EmailVerificationTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailVerificationTokens
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EmailVerificationTokens PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        TokenHash NVARCHAR(64) NOT NULL,
        ExpiresAtUtc DATETIME2 NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        UsedAtUtc DATETIME2 NULL,
        CONSTRAINT FK_EmailVerificationTokens_User FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id)
    );
    CREATE UNIQUE INDEX UX_EmailVerificationTokens_Hash ON dbo.EmailVerificationTokens(TokenHash);
END;
