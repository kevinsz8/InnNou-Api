-- =============================================================
-- MIGRATION: Add LastUpdatedUtc / LastUpdatedBy to catalog tables
-- Date: 2026-06-27
-- =============================================================
-- Affected tables:
--   UnitTypes, UnitsOfMeasure, UnitConversionRates,
--   Families, SubFamilies, Categories, SubCategories
-- =============================================================

BEGIN TRANSACTION;

-- UnitTypes
ALTER TABLE UnitTypes ADD LastUpdatedUtc datetime2    NULL;
ALTER TABLE UnitTypes ADD LastUpdatedBy  nvarchar(150) NULL;

-- UnitsOfMeasure
ALTER TABLE UnitsOfMeasure ADD LastUpdatedUtc datetime2    NULL;
ALTER TABLE UnitsOfMeasure ADD LastUpdatedBy  nvarchar(150) NULL;

-- UnitConversionRates
ALTER TABLE UnitConversionRates ADD LastUpdatedUtc datetime2    NULL;
ALTER TABLE UnitConversionRates ADD LastUpdatedBy  nvarchar(150) NULL;

-- Families
ALTER TABLE Families ADD LastUpdatedUtc datetime2    NULL;
ALTER TABLE Families ADD LastUpdatedBy  nvarchar(150) NULL;

-- SubFamilies
ALTER TABLE SubFamilies ADD LastUpdatedUtc datetime2    NULL;
ALTER TABLE SubFamilies ADD LastUpdatedBy  nvarchar(150) NULL;

-- Categories
ALTER TABLE Categories ADD LastUpdatedUtc datetime2    NULL;
ALTER TABLE Categories ADD LastUpdatedBy  nvarchar(150) NULL;

-- SubCategories
ALTER TABLE SubCategories ADD LastUpdatedUtc datetime2    NULL;
ALTER TABLE SubCategories ADD LastUpdatedBy  nvarchar(150) NULL;

COMMIT TRANSACTION;
PRINT '=== Migration 20260627_CatalogTables_AddUpdateAudit completed successfully ===';
