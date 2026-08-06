/* =============================================================
   ARTICLES — default receiving unit (Goods Receipts convenience default)
   Date: 2026-08-06
   =============================================================
   Researched before building (per feedback_research_before_deciding): SAP MIGO proposes the
   PO's own Order Unit as the "unit of entry" default at goods receipt, but the receiver can
   freely switch to any alternative unit already defined in the material master (MARM) — it is
   never locked to one mandatory unit, since what physically arrives varies shipment to
   shipment. Oracle Purchasing and Odoo behave the same way. This column is exactly that: a
   pure pre-fill convenience for ReceiveGoodsPage's own unit picker (see
   InnNou.Application.Common.ArticleUnitConversion / migrations/
   20260806_GoodsReceiptLine_UnitConversion.sql) — NULL means "default to the Purchase Unit"
   (today's behavior, backward compatible); a receiver can always still pick a different valid
   unit for that specific receipt. Same "write-in token resolved server-side, denormalized on
   read" shape as Articles.TaxCategoryId.

   Validated at Create/Edit time against the article's own valid unit universe (Purchase Unit or
   a level in its own ArticlePackagingLevels chain — ArticleUnitConversion.GetRequestableUnitIds)
   — never an arbitrary UnitOfMeasure. Not itself a structural field (EditArticleCommandHandler's
   structural-change check does not need to include it), since changing which already-valid unit
   is the default doesn't change the article's own packaging chain.

   Idempotent — safe to re-run.
   ============================================================= */

IF COL_LENGTH('dbo.Articles', 'DefaultReceivingUnitId') IS NULL
BEGIN
    ALTER TABLE dbo.Articles ADD
        DefaultReceivingUnitId INT NULL CONSTRAINT FK_Articles_DefaultReceivingUnit REFERENCES dbo.UnitsOfMeasure(UnitOfMeasureId);
END;
GO

PRINT '=== Migration 20260806_Articles_AddDefaultReceivingUnit completed successfully ===';
GO
