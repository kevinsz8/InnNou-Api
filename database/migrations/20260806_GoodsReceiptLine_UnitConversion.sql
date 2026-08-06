/* =============================================================
   GOODS RECEIPTS — unit-of-issue conversion (Aceptado/Cortesia/Rechazado)
   Date: 2026-08-06
   =============================================================
   Extends the 2026-08-06 "unit-aware quantities" feature (see
   migrations/20260806_UnitAwareQuantities_AddColumns.sql and
   InnNou.Application.Common.ArticleUnitConversion) to Goods Receipts —
   researched against SAP MIGO (Alternative Unit of Measure, distinct from
   the PO's own Order Unit), Oracle Purchasing (receipt UOM auto-converts
   back to the PO's ordered UOM), and Odoo (UoM changeable at receipt
   validation within the same category) before building. All three keep the
   Purchase Order's own contract unit fixed and only let the *receiving
   count* itself use a different unit — same boundary this codebase already
   draws for Requisitions/Inventory vs. Orders/PurchaseOrders. Confirmed with
   the user: scoped ONLY to Accepted/Courtesy/Rejected — the PurchaseOrder/
   PurchaseOrderLine stay denominated in the supplier's PurchaseUnitId, never
   touched here.

   One shared EnteredUnitId per line (not three) — a receiver physically
   counts Accepted/Courtesy/Rejected from the same opened container in the
   same unit, so a single per-line unit choice matches how receiving
   actually happens. Same "normalize + keep the original" storage strategy
   as every other table in this feature: QuantityAccepted/Courtesy/Rejected
   stay exactly as they are today (always PurchaseUnitId-normalized) and
   continue to drive PO-status recompute, over-receipt capping, stock
   deltas, and tax computation unchanged; the three new *QuantityInUnit
   columns are purely the raw as-entered mirror, NULL meaning "entered
   directly in the Purchase Unit."

   GoodsReceiptLineTableType (see migrations/20260804_GoodsReceiptBatch_
   CreateTableTypes.sql) has no ALTER TYPE in SQL Server — dropped and
   recreated here with the new columns, which requires dropping
   sp_GoodsReceiptLine_CreateBatch first (a table type can't be dropped
   while a procedure parameter still references it) and re-deploying the
   procedure's own .sql file (CREATE OR ALTER) immediately after this
   migration runs.

   Idempotent — safe to re-run.
   ============================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.GoodsReceiptLine', 'EnteredUnitId') IS NULL
BEGIN
    ALTER TABLE dbo.GoodsReceiptLine ADD
        EnteredUnitId          INT           NULL CONSTRAINT FK_GoodsReceiptLine_EnteredUnit REFERENCES dbo.UnitsOfMeasure(UnitOfMeasureId),
        AcceptedQuantityInUnit DECIMAL(18,8) NULL,
        CourtesyQuantityInUnit DECIMAL(18,8) NULL,
        RejectedQuantityInUnit DECIMAL(18,8) NULL;
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

PRINT '=== Migration 20260806_GoodsReceiptLine_UnitConversion completed successfully ===';
GO
