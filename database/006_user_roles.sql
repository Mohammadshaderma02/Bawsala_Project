IF COL_LENGTH('dbo.ClientAccounts', 'Role') IS NULL
BEGIN
    ALTER TABLE dbo.ClientAccounts
    ADD Role NVARCHAR(20) NOT NULL
        CONSTRAINT DF_ClientAccounts_Role DEFAULT N'Client';
END;

GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ClientAccounts_Role'
)
BEGIN
    ALTER TABLE dbo.ClientAccounts
    ADD CONSTRAINT CK_ClientAccounts_Role CHECK (Role IN (N'Admin', N'Client'));
END;

-- After registering the first administrator through /login, promote it explicitly:
-- UPDATE dbo.ClientAccounts SET Role = N'Admin' WHERE Email = N'admin@example.com';
