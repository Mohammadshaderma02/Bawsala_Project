IF OBJECT_ID(N'dbo.Assets', N'U') IS NULL
    THROW 50011, 'dbo.Assets must exist before migration 011 can run.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Assets')
      AND c.name = N'IsActive'
)
BEGIN
    ALTER TABLE dbo.Assets
        ADD CONSTRAINT DF_Assets_IsActive DEFAULT (1) FOR IsActive;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Assets')
      AND c.name = N'CreatedAtUtc'
)
BEGIN
    ALTER TABLE dbo.Assets
        ADD CONSTRAINT DF_Assets_CreatedAtUtc DEFAULT (SYSUTCDATETIME()) FOR CreatedAtUtc;
END;
