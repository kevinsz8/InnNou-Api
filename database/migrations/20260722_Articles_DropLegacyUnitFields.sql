-- =============================================================
-- MIGRATION: Drop Articles' legacy 2-level unit fields
-- Date: 2026-07-22
-- =============================================================
-- PurchaseQuantity/ContentUnitId/ContentQuantity/BaseUnitId are
-- superseded by ArticlePackagingLevels (see the two migrations
-- immediately before this one — this one MUST run last, after the
-- backfill has copied their data forward). PurchaseUnitId stays on
-- Articles unchanged — it's still the unit the article is bought/
-- priced by; everything inside it now lives in the levels chain.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Articles_ContentUnit')
BEGIN
    ALTER TABLE Articles DROP CONSTRAINT FK_Articles_ContentUnit;
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Articles_BaseUnit')
BEGIN
    ALTER TABLE Articles DROP CONSTRAINT FK_Articles_BaseUnit;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'PurchaseQuantity')
BEGIN
    ALTER TABLE Articles DROP COLUMN PurchaseQuantity;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'ContentUnitId')
BEGIN
    ALTER TABLE Articles DROP COLUMN ContentUnitId;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'ContentQuantity')
BEGIN
    ALTER TABLE Articles DROP COLUMN ContentQuantity;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'BaseUnitId')
BEGIN
    ALTER TABLE Articles DROP COLUMN BaseUnitId;
END
GO
