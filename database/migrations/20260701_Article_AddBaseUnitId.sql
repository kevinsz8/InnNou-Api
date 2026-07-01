-- =============================================================
-- MIGRATION: Add Articles.BaseUnitId
-- Date: 2026-07-01
-- =============================================================
-- Retroactive migration: BaseUnitId already exists on the live
-- InnNou DB (added directly, without a tracked migration) and is
-- referenced by sp_Article_Create/Update/GetByToken/GetPaged.
-- This migration brings fresh environments in line and documents
-- the change. Guarded so it is a no-op if already applied.
-- =============================================================

IF COL_LENGTH('Articles', 'BaseUnitId') IS NULL
BEGIN
    ALTER TABLE Articles ADD BaseUnitId int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Articles_BaseUnit')
BEGIN
    ALTER TABLE Articles
        ADD CONSTRAINT FK_Articles_BaseUnit
        FOREIGN KEY (BaseUnitId) REFERENCES UnitsOfMeasure (UnitOfMeasureId);
END
GO
