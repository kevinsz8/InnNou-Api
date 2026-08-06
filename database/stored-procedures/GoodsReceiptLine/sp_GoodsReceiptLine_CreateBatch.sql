SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPTLINE - CREATE BATCH
   Table-valued-parameter sibling of sp_GoodsReceiptLine_Create — inserts every accepted line for
   one Goods Receipt in a single round trip instead of one call per line, called from
   PurchaseOrderService.CreateGoodsReceiptAsync's transaction. See
   migrations/20260804_GoodsReceiptBatch_CreateTableTypes.sql for the @Lines table type.

   Unlike sp_PurchaseOrderLine_CreateBatch, the caller DOES need the generated
   GoodsReceiptLineId back per row — it's the FK the subsequent InventoryMovement batch insert
   needs (sp_InventoryMovement_CreateBatch's @GoodsReceiptLineId column). Returned keyed by the
   caller-supplied GoodsReceiptLineToken via OUTPUT INTO, not the full re-select
   sp_GoodsReceiptLine_Create does — the caller already has every other field client-side.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceiptLine_CreateBatch
(
    @Lines     dbo.GoodsReceiptLineTableType READONLY,
    @CreatedBy VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Inserted TABLE (GoodsReceiptLineToken UNIQUEIDENTIFIER, GoodsReceiptLineId INT);

    INSERT INTO dbo.GoodsReceiptLine
        (GoodsReceiptLineToken, GoodsReceiptId, PurchaseOrderLineId, ArticleId,
         QuantityAccepted, QuantityCourtesy, QuantityRejected, RejectionReason,
         LotNumber, ExpirationDate, SerialNumber, Notes,
         TaxCategoryId, TaxRateId, TaxRatePercent, TaxableAmount, TaxAmount, TotalAmount,
         EnteredUnitId, AcceptedQuantityInUnit, CourtesyQuantityInUnit, RejectedQuantityInUnit,
         CreatedBy)
    OUTPUT INSERTED.GoodsReceiptLineToken, INSERTED.GoodsReceiptLineId INTO @Inserted
    SELECT
        GoodsReceiptLineToken, GoodsReceiptId, PurchaseOrderLineId, ArticleId,
        QuantityAccepted, QuantityCourtesy, QuantityRejected, RejectionReason,
        LotNumber, ExpirationDate, SerialNumber, Notes,
        TaxCategoryId, TaxRateId, TaxRatePercent, TaxableAmount, TaxAmount, TotalAmount,
        EnteredUnitId, AcceptedQuantityInUnit, CourtesyQuantityInUnit, RejectedQuantityInUnit,
        @CreatedBy
    FROM @Lines;

    SELECT GoodsReceiptLineToken, GoodsReceiptLineId FROM @Inserted;
END;
GO
