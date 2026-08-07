SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDERLINE - CREATE BATCH
   Table-valued-parameter sibling of sp_PurchaseOrderLine_Create — inserts every
   PurchaseOrderLine for one supplier's split in a single round trip instead of one call per
   OrderLine, called from OrderService.CompleteSubmissionAsync's split transaction. See
   migrations/20260804_PurchaseOrderLine_CreateBatchTableType.sql for the @Lines table type.
   Caller doesn't need the created rows back (CompleteSubmissionAsync never reads them), so this
   is a plain batch INSERT — no OUTPUT/join-back needed, unlike the single-row Create SP.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderLine_CreateBatch
(
    @Lines     dbo.PurchaseOrderLineTableType READONLY,
    @CreatedBy VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PurchaseOrderLine
        (PurchaseOrderLineToken, PurchaseOrderId, OrderLineId, ArticleId, Quantity,
         PurchaseUnitId, PurchaseQuantity, ContentUnitId, ContentQuantity,
         UnitPrice, CurrencyCode, CategoryId, CategoryCode, SubCategoryId, SubCategoryCode,
         BaseUnitPrice, DiscountTypeId, DiscountValue, Notes, CreatedBy)
    SELECT
        PurchaseOrderLineToken, PurchaseOrderId, OrderLineId, ArticleId, Quantity,
        PurchaseUnitId, PurchaseQuantity, ContentUnitId, ContentQuantity,
        UnitPrice, CurrencyCode, CategoryId, CategoryCode, SubCategoryId, SubCategoryCode,
        BaseUnitPrice, DiscountTypeId, DiscountValue, Notes, @CreatedBy
    FROM @Lines;
END;
GO
