-- =============================================================
-- MIGRATION: Drop PurposeCode from Warehouses
-- Date: 2026-07-22
-- =============================================================
-- PurposeCode (GENERAL/KITCHEN/BAR/...) was purely informational —
-- nothing in the app ever branched on it, no filter/report ever
-- consumed it, and every real behavior distinction is already
-- covered by Warehouse's capability BIT columns. A fixed
-- CHECK-constrained enum requiring a migration + deployment for
-- every new value, with zero consumers, isn't worth keeping —
-- the Warehouse Name already identifies the location. Guarded so
-- it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Warehouses_PurposeCode' AND parent_object_id = OBJECT_ID('Warehouses'))
BEGIN
    ALTER TABLE Warehouses DROP CONSTRAINT CK_Warehouses_PurposeCode;
END
GO

-- The column's DEFAULT ('GENERAL') was created inline, so SQL Server
-- auto-named the constraint (e.g. DF__Warehouse__Purpo__xxxxxxxx) —
-- look it up dynamically rather than trusting a literal name, same
-- gotcha already documented for Categories.Code in this file's history.
DECLARE @DefaultConstraintName SYSNAME;
SELECT @DefaultConstraintName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('Warehouses') AND c.name = 'PurposeCode';

IF @DefaultConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE Warehouses DROP CONSTRAINT [' + @DefaultConstraintName + ']');
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Warehouses') AND name = 'PurposeCode')
BEGIN
    ALTER TABLE Warehouses DROP COLUMN PurposeCode;
END
GO
