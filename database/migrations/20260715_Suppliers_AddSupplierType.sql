-- =============================================================
-- MIGRATION: Add Suppliers.SupplierType (Product/Service/Mixed)
-- Date: 2026-07-15
-- =============================================================
-- Informational-only classification, same shape as Warehouses.PurposeCode
-- (plain CHECK-constrained column, no lookup table) — identifies whether a
-- supplier sells products, offers services, or both. Not used to drive any
-- business logic yet (Article's fixed-unit purchase/content structure still
-- assumes a product; modeling variable-priced services properly, e.g. for a
-- supplier like an electrical-panel-maintenance contractor, is a separate,
-- not-yet-designed follow-up — this migration only adds the classification).
--
-- Defaults every existing and new row to 'PRODUCT', since that's the
-- implicit assumption the whole Article/ArticlePrice model has made so far.
--
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('Suppliers', 'SupplierType') IS NULL
BEGIN
    ALTER TABLE Suppliers ADD SupplierType varchar(20) NOT NULL DEFAULT ('PRODUCT');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Suppliers_SupplierType')
BEGIN
    ALTER TABLE Suppliers ADD CONSTRAINT CK_Suppliers_SupplierType
        CHECK (SupplierType IN (N'PRODUCT', N'SERVICE', N'MIXED'));
END
GO
