/* =============================================================
   GOODS RECEIPT LINE — freeze UnitPrice + CurrencyCode for every received line
   Date: 2026-08-07
   =============================================================
   Follow-up while researching Fase C (Notas de Crédito): TaxableAmount/
   TaxAmount/TotalAmount were already frozen at receipt time, but only over
   QuantityAccepted, and only the already-multiplied total ever survived —
   the UnitPrice used to compute it (PurchaseOrderService.CreateGoodsReceiptAsync's
   own `validated.Line.UnitPrice`) was read and discarded. A line with
   QuantityAccepted = 0 (a 100%-rejected delivery) got NO tax computation
   at all, so nothing frozen anywhere could later value what was rejected —
   exactly the number a Nota de Crédito needs. See
   .claude/ArticleUnitConversionModule.md's "Price comparison report"
   section for the sibling finding that led here.

   UnitPrice/CurrencyCode are frozen for EVERY received line unconditionally
   (no tax dependency — they're just "what we agreed to pay, and in what
   currency"; a UnitPrice with no CurrencyCode is meaningless once more than
   one currency exists in the system, e.g. Costa Rica/El Salvador). Tax
   category/rate/amounts stay exactly as strict as before for a billable
   line (QuantityAccepted > 0, hard error on missing config), but are now
   also computed best-effort (never blocking) for a rejected-only line when
   tax IS configured — see PurchaseOrderService.CreateGoodsReceiptAsync.

   GoodsReceiptLineTableType (see migrations/20260804_GoodsReceiptBatch_
   CreateTableTypes.sql, evolved in 20260806_GoodsReceiptLine_UnitConversion.sql)
   has no ALTER TYPE in SQL Server — dropped and recreated here with the new
   columns, which requires dropping sp_GoodsReceiptLine_CreateBatch first (a
   table type can't be dropped while a procedure parameter still references
   it) and re-deploying the procedure's own .sql file (CREATE OR ALTER)
   immediately after this migration runs.

   Idempotent — safe to re-run (each column guarded separately, since this
   file was extended with CurrencyCode after UnitPrice had already been
   deployed once).
   ============================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.GoodsReceiptLine', 'UnitPrice') IS NULL
BEGIN
    ALTER TABLE dbo.GoodsReceiptLine ADD
        UnitPrice DECIMAL(18,8) NULL;
END;
GO

IF COL_LENGTH('dbo.GoodsReceiptLine', 'CurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.GoodsReceiptLine ADD
        CurrencyCode VARCHAR(10) NULL;
END;
GO

IF OBJECT_ID('dbo.sp_GoodsReceiptLine_CreateBatch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GoodsReceiptLine_CreateBatch;
GO

IF TYPE_ID(N'dbo.GoodsReceiptLineTableType') IS NOT NULL
    DROP TYPE dbo.GoodsReceiptLineTableType;
GO

CREATE TYPE dbo.GoodsReceiptLineTableType AS TABLE
(
    GoodsReceiptLineToken  UNIQUEIDENTIFIER NOT NULL,
    GoodsReceiptId         INT               NOT NULL,
    PurchaseOrderLineId    INT               NOT NULL,
    ArticleId              INT               NOT NULL,
    QuantityAccepted       DECIMAL(18,8)     NOT NULL,
    QuantityCourtesy       DECIMAL(18,8)     NOT NULL,
    QuantityRejected       DECIMAL(18,8)     NOT NULL,
    RejectionReason        NVARCHAR(500)     NULL,
    LotNumber              NVARCHAR(100)     NULL,
    ExpirationDate         DATE              NULL,
    SerialNumber           NVARCHAR(100)     NULL,
    Notes                  NVARCHAR(500)     NULL,
    UnitPrice              DECIMAL(18,8)     NULL,
    CurrencyCode           VARCHAR(10)       NULL,
    TaxCategoryId          INT               NULL,
    TaxRateId              INT               NULL,
    TaxRatePercent         DECIMAL(11,8)     NULL,
    TaxableAmount          DECIMAL(18,8)     NULL,
    TaxAmount              DECIMAL(18,8)     NULL,
    TotalAmount            DECIMAL(18,8)     NULL,
    EnteredUnitId          INT               NULL,
    AcceptedQuantityInUnit DECIMAL(18,8)     NULL,
    CourtesyQuantityInUnit DECIMAL(18,8)     NULL,
    RejectedQuantityInUnit DECIMAL(18,8)     NULL
);
GO

-- sp_GoodsReceiptLine_CreateBatch.sql must be re-deployed right after this migration —
-- dropped above to satisfy the TVP drop, and this script does not recreate it.

PRINT '=== Migration 20260807_GoodsReceiptLine_AddUnitPrice completed successfully ===';
GO
