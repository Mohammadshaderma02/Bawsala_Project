IF OBJECT_ID(N'dbo.JourneyProducts', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.JourneyProducts
    SET Currency = N'JOD',
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Currency = N'USD';
END;

IF OBJECT_ID(N'dbo.PaymentTransactions', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.PaymentTransactions
    SET Currency = N'JOD'
    WHERE Provider = N'meps'
      AND Status = N'pending'
      AND Currency = N'USD';
END;

IF OBJECT_ID(N'dbo.JourneyOrders', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.JourneyOrders
    SET Currency = N'JOD'
    WHERE Status = N'pending'
      AND Currency = N'USD';
END;

IF OBJECT_ID(N'dbo.LandingPages', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.LandingPages
    SET ContentJson = REPLACE(REPLACE(ContentJson, N'"currency":"USD"', N'"currency":"JOD"'), N'"currency": "USD"', N'"currency": "JOD"'),
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE ContentJson LIKE N'%"currency"%USD%';
END;
