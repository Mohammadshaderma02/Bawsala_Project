IF OBJECT_ID(N'dbo.ClientAccounts', N'U') IS NULL
BEGIN
CREATE TABLE dbo.ClientAccounts
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ClientAccounts PRIMARY KEY,
    Name NVARCHAR(160) NOT NULL,
    Email NVARCHAR(320) NOT NULL,
    PasswordHash NVARCHAR(100) NOT NULL,
    PasswordSalt NVARCHAR(100) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_ClientAccounts_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_ClientAccounts_Email UNIQUE (Email)
);
END;

IF OBJECT_ID(N'dbo.ClientJourneyStates', N'U') IS NULL
BEGIN
CREATE TABLE dbo.ClientJourneyStates
(
    UserId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ClientJourneyStates PRIMARY KEY,
    StateJson NVARCHAR(MAX) NOT NULL,
    UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_ClientJourneyStates_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ClientJourneyStates_ClientAccounts FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id),
    CONSTRAINT CK_ClientJourneyStates_StateJson_IsJson CHECK (ISJSON(StateJson) = 1)
);
END;
