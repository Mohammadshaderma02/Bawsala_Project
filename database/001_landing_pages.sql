CREATE TABLE dbo.LandingPages
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LandingPages PRIMARY KEY,
    LanguageCode NVARCHAR(5) NOT NULL,
    ContentJson NVARCHAR(MAX) NOT NULL,
    IsPublished BIT NOT NULL CONSTRAINT DF_LandingPages_IsPublished DEFAULT (1),
    CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_LandingPages_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_LandingPages_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_LandingPages_ContentJson_IsJson CHECK (ISJSON(ContentJson) = 1)
);

CREATE INDEX IX_LandingPages_LanguageCode_IsPublished_UpdatedAtUtc
ON dbo.LandingPages(LanguageCode, IsPublished, UpdatedAtUtc DESC);

