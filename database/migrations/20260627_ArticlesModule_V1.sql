-- =============================================================
-- MIGRATION: Articles Module V1 schema alignment
-- Date: 2026-06-27
-- =============================================================
-- Changes:
--   1. Drop HotelArticleOverrides (deferred to V2)
--   2. Articles: ContentUnitId/ContentQuantity NOT NULL;
--               add FamilyId, SubFamilyId, MinimumOrderQty, LeadTimeDays
--   3. ArticlePrices: replace EffectiveFromUtc/EffectiveToUtc with EffectiveDate (date);
--                     add CurrencyCode, HotelId (contract pricing), Notes
--   4. HotelArticles: rename IsEnabled -> IsActive
-- =============================================================
-- NOTE: UPDATEs after ALTER TABLE ADD use EXEC() to avoid
--       batch-level column-binding errors in SQL Server.
-- =============================================================

BEGIN TRANSACTION;

-- ============================================================
-- 1. Drop HotelArticleOverrides (deferred to V2)
-- ============================================================
PRINT '--- Step 1: Drop HotelArticleOverrides ---';

ALTER TABLE HotelArticleOverrides DROP CONSTRAINT FK_HotelArticleOverrides_HotelArticles;
ALTER TABLE HotelArticleOverrides DROP CONSTRAINT FK_HotelArticleOverrides_Hotels;
DROP TABLE HotelArticleOverrides;

-- ============================================================
-- 2. Articles: tighten nullability + add new columns
-- ============================================================
PRINT '--- Step 2: Articles ---';

-- 2a. Handle any NULLs before tightening nullability
UPDATE Articles SET ContentUnitId   = PurchaseUnitId WHERE ContentUnitId   IS NULL;
UPDATE Articles SET ContentQuantity = 1              WHERE ContentQuantity IS NULL;

ALTER TABLE Articles ALTER COLUMN ContentUnitId    int           NOT NULL;
ALTER TABLE Articles ALTER COLUMN ContentQuantity  decimal(18,6) NOT NULL;

-- 2b. Supplier default classification + procurement hints
ALTER TABLE Articles ADD FamilyId        int           NULL;
ALTER TABLE Articles ADD SubFamilyId     int           NULL;
ALTER TABLE Articles ADD MinimumOrderQty decimal(18,4) NULL;
ALTER TABLE Articles ADD LeadTimeDays    int           NULL;

-- 2c. FK constraints for classification columns
ALTER TABLE Articles
    ADD CONSTRAINT FK_Articles_FamilyId
    FOREIGN KEY (FamilyId) REFERENCES Families(FamilyId);

ALTER TABLE Articles
    ADD CONSTRAINT FK_Articles_SubFamilyId
    FOREIGN KEY (SubFamilyId) REFERENCES SubFamilies(SubFamilyId);

-- ============================================================
-- 3. ArticlePrices: insert-only redesign + contract pricing
-- ============================================================
PRINT '--- Step 3: ArticlePrices ---';

-- 3a. Add new columns (nullable first)
ALTER TABLE ArticlePrices ADD EffectiveDate date          NULL;
ALTER TABLE ArticlePrices ADD CurrencyCode  nvarchar(3)   NULL;
ALTER TABLE ArticlePrices ADD HotelId       int           NULL;
ALTER TABLE ArticlePrices ADD Notes         nvarchar(500) NULL;

-- 3b. Populate from old columns (EXEC avoids batch-level compile error on new columns)
EXEC(N'UPDATE ArticlePrices
       SET    EffectiveDate = CAST(EffectiveFromUtc AS date),
              CurrencyCode  = ''EUR''
       WHERE  EffectiveDate IS NULL');

-- 3c. Enforce NOT NULL
ALTER TABLE ArticlePrices ALTER COLUMN EffectiveDate date        NOT NULL;
ALTER TABLE ArticlePrices ALTER COLUMN CurrencyCode  nvarchar(3) NOT NULL;

-- 3d. Drop the old range columns (no default constraints on them)
ALTER TABLE ArticlePrices DROP COLUMN EffectiveFromUtc;
ALTER TABLE ArticlePrices DROP COLUMN EffectiveToUtc;

-- 3e. FK for contract-pricing hotel link
ALTER TABLE ArticlePrices
    ADD CONSTRAINT FK_ArticlePrices_HotelId
    FOREIGN KEY (HotelId) REFERENCES Hotels(HotelId);

-- ============================================================
-- 4. HotelArticles: rename IsEnabled -> IsActive
-- ============================================================
PRINT '--- Step 4: HotelArticles ---';

-- 4a. Drop auto-generated default so we can re-add with a proper name
ALTER TABLE HotelArticles DROP CONSTRAINT DF__HotelArti__IsEna__21A0F6C4;

-- 4b. Rename
EXEC sp_rename 'HotelArticles.IsEnabled', 'IsActive', 'COLUMN';

-- 4c. Re-add default with a clean, stable name
ALTER TABLE HotelArticles
    ADD CONSTRAINT DF_HotelArticles_IsActive DEFAULT (1) FOR IsActive;

-- ============================================================
COMMIT TRANSACTION;
PRINT '=== Migration 20260627_ArticlesModule_V1 completed successfully ===';
