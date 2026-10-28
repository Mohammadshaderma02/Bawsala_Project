IF OBJECT_ID(N'dbo.PaymentTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentTransactions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PaymentTransactions PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NULL,
        CustomerName NVARCHAR(160) NOT NULL,
        CustomerEmail NVARCHAR(320) NOT NULL,
        ServiceType NVARCHAR(40) NOT NULL,
        ProductSlug NVARCHAR(120) NOT NULL,
        ProductTitle NVARCHAR(240) NOT NULL,
        Amount DECIMAL(12,2) NOT NULL,
        Currency NCHAR(3) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        Provider NVARCHAR(60) NOT NULL,
        ProviderReference NVARCHAR(300) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        PaidAtUtc DATETIME2 NULL,
        RefundedAtUtc DATETIME2 NULL,
        RefundReason NVARCHAR(1000) NOT NULL CONSTRAINT DF_PaymentTransactions_RefundReason DEFAULT N'',
        CONSTRAINT FK_PaymentTransactions_User FOREIGN KEY (UserId) REFERENCES dbo.ClientAccounts(Id),
        CONSTRAINT CK_PaymentTransactions_Status CHECK (Status IN (N'pending',N'paid',N'failed',N'refunded',N'cancelled')),
        CONSTRAINT CK_PaymentTransactions_Amount CHECK (Amount >= 0)
    );
    CREATE INDEX IX_PaymentTransactions_Created ON dbo.PaymentTransactions(CreatedAtUtc DESC);
    CREATE INDEX IX_PaymentTransactions_User ON dbo.PaymentTransactions(UserId, CreatedAtUtc DESC);
END;
