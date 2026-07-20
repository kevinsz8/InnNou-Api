-- =============================================================
-- MIGRATION: Backfill ArticlePackagingLevels from legacy Article fields
-- Date: 2026-07-22
-- =============================================================
-- Must run AFTER 20260722_ArticlePackagingLevels_Create.sql and
-- BEFORE 20260722_Articles_DropLegacyUnitFields.sql — it reads the
-- old Articles.PurchaseQuantity/ContentUnitId/ContentQuantity
-- columns that the next migration removes.
--
-- Every existing Article had PurchaseQuantity = 1 at the time this
-- was written (verified live), so the common case collapses to a
-- single "Unidad Definida" row per article (old ContentUnit/
-- ContentQuantity become the terminal level). The PurchaseQuantity
-- <> 1 branch is included for correctness in case that's no longer
-- true by the time this runs elsewhere — it introduces a synthetic
-- "PIECE" intermediate count level, since the old schema never gave
-- that implicit count a name of its own (see CLAUDE.md's "Article
-- packaging levels" section for why this is a known, accepted
-- migration lossiness — real article names like "Botella" are not
-- recoverable automatically and should be corrected by hand).
--
-- Guarded so it only inserts for articles that don't already have
-- packaging levels (safe to re-run).
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @GenericCountUnitId INT = (SELECT TOP 1 UnitOfMeasureId FROM UnitsOfMeasure WHERE Code = 'PIECE');

-- Single-level case: PurchaseQuantity = 1, so the old ContentUnit/ContentQuantity
-- already fully describe the article — becomes the one-and-only Defined level.
INSERT INTO ArticlePackagingLevels (ArticlePackagingLevelToken, ArticleId, SequenceOrder, UnitOfMeasureId, QuantityInParentUnit, IsDefinedUnit, CreatedBy)
SELECT
    NEWID(),
    a.ArticleId,
    1,
    a.ContentUnitId,
    ISNULL(a.ContentQuantity, 1),
    1,
    a.CreatedBy
FROM Articles a
WHERE a.PurchaseQuantity = 1
  AND NOT EXISTS (SELECT 1 FROM ArticlePackagingLevels apl WHERE apl.ArticleId = a.ArticleId);

-- Multi-level case: PurchaseQuantity <> 1 means there's a real intermediate
-- count (e.g. "24 bottles") the old schema never named — approximated here
-- with a generic PIECE unit; SequenceOrder 1 holds the count, 2 is the
-- Defined terminal level from the old ContentUnit/ContentQuantity.
INSERT INTO ArticlePackagingLevels (ArticlePackagingLevelToken, ArticleId, SequenceOrder, UnitOfMeasureId, QuantityInParentUnit, IsDefinedUnit, CreatedBy)
SELECT
    NEWID(),
    a.ArticleId,
    1,
    @GenericCountUnitId,
    a.PurchaseQuantity,
    0,
    a.CreatedBy
FROM Articles a
WHERE a.PurchaseQuantity <> 1
  AND @GenericCountUnitId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM ArticlePackagingLevels apl WHERE apl.ArticleId = a.ArticleId);

INSERT INTO ArticlePackagingLevels (ArticlePackagingLevelToken, ArticleId, SequenceOrder, UnitOfMeasureId, QuantityInParentUnit, IsDefinedUnit, CreatedBy)
SELECT
    NEWID(),
    a.ArticleId,
    2,
    a.ContentUnitId,
    ISNULL(a.ContentQuantity, 1),
    1,
    a.CreatedBy
FROM Articles a
WHERE a.PurchaseQuantity <> 1
  AND @GenericCountUnitId IS NOT NULL
  AND EXISTS (SELECT 1 FROM ArticlePackagingLevels apl WHERE apl.ArticleId = a.ArticleId AND apl.SequenceOrder = 1 AND apl.IsDefinedUnit = 0)
  AND NOT EXISTS (SELECT 1 FROM ArticlePackagingLevels apl WHERE apl.ArticleId = a.ArticleId AND apl.SequenceOrder = 2);
GO
