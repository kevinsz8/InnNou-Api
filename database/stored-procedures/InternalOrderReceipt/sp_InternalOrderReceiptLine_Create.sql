SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERRECEIPTLINE - CREATE
   Single-line insert + re-select, called once per line in a C# loop inside
   InternalOrderService.CreateReceiptAsync's shared transaction, right after
   the stock-in effect (sp_StockLevel_ApplyDelta + sp_InventoryMovement_Create,
   Type = INTERNAL_ORDER_IN) is applied for the same line — same
   one-call-per-line shape as sp_GoodsReceiptLine_Create (no TVP/batch
   parameter, this is a lower-volume flow).

   2-way Accepted/Rejected split only (no Courtesy, see the migration header
   note for why). QuantityAccepted is validated by the caller against the
   line's remaining QuantityShipped - QuantityReceived on the referenced
   InternalOrderShipmentLine before this call; the DB-layer CHECK constraints
   (non-negative, not-both-zero, rejection reason required) are the backstop.

   Tax params are frozen here exactly like GoodsReceiptLine's own — resolved
   in C# from the destination Warehouse's TaxJurisdiction + the Article's
   effective TaxCategory, never recomputed later.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderReceiptLine_Create
(
    @InternalOrderReceiptLineToken UNIQUEIDENTIFIER,
    @InternalOrderReceiptId        INT,
    @InternalOrderShipmentLineId   INT,
    @QuantityAccepted              DECIMAL(18,8) = 0,
    @QuantityRejected              DECIMAL(18,8) = 0,
    @RejectionReason               NVARCHAR(500) = NULL,
    @TaxCategoryId                 INT           = NULL,
    @TaxRateId                     INT           = NULL,
    @TaxRatePercent                DECIMAL(11,8) = NULL,
    @TaxableAmount                 DECIMAL(18,8) = NULL,
    @TaxAmount                     DECIMAL(18,8) = NULL,
    @TotalAmount                   DECIMAL(18,8) = NULL,
    @Notes                         NVARCHAR(500) = NULL,
    @CreatedBy                     VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InternalOrderReceiptLines
        (InternalOrderReceiptLineToken, InternalOrderReceiptId, InternalOrderShipmentLineId,
         QuantityAccepted, QuantityRejected, RejectionReason,
         TaxCategoryId, TaxRateId, TaxRatePercent, TaxableAmount, TaxAmount, TotalAmount,
         Notes, CreatedBy)
    VALUES
        (@InternalOrderReceiptLineToken, @InternalOrderReceiptId, @InternalOrderShipmentLineId,
         @QuantityAccepted, @QuantityRejected, @RejectionReason,
         @TaxCategoryId, @TaxRateId, @TaxRatePercent, @TaxableAmount, @TaxAmount, @TotalAmount,
         @Notes, @CreatedBy);

    SELECT
        iorl.InternalOrderReceiptLineId, iorl.InternalOrderReceiptLineToken, iorl.InternalOrderReceiptId,
        iorl.InternalOrderShipmentLineId, iosl.InternalOrderShipmentLineToken, iosl.QuantityShipped,
        iol.InternalOrderLineId, iol.InternalOrderLineToken,
        a.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        iorl.QuantityAccepted, iorl.QuantityRejected, iorl.RejectionReason,
        iorl.TaxCategoryId, tc.Code AS TaxCategoryCode, iorl.TaxRatePercent,
        iorl.TaxableAmount, iorl.TaxAmount, iorl.TotalAmount,
        iorl.Notes,
        iorl.CreatedUtc, iorl.CreatedBy
    FROM dbo.InternalOrderReceiptLines iorl
    JOIN dbo.InternalOrderShipmentLines iosl ON iosl.InternalOrderShipmentLineId = iorl.InternalOrderShipmentLineId
    JOIN dbo.InternalOrderLines iol           ON iol.InternalOrderLineId          = iosl.InternalOrderLineId
    JOIN dbo.Articles a                       ON a.ArticleId                      = iol.ArticleId
    LEFT JOIN dbo.TaxCategories tc            ON tc.TaxCategoryId                 = iorl.TaxCategoryId
    WHERE iorl.InternalOrderReceiptLineToken = @InternalOrderReceiptLineToken;
END;
GO
