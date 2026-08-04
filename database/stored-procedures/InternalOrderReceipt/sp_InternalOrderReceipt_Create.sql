SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERRECEIPT - CREATE
   Header insert + re-select only — no transaction here, the C# service
   (InternalOrderService.CreateReceiptAsync) owns the transaction spanning
   this header insert, the stock-in effect (sp_StockLevel_ApplyDelta +
   sp_InventoryMovement_Create, Type = INTERNAL_ORDER_IN), the per-line
   sp_InternalOrderReceiptLine_Create calls, and the
   sp_InternalOrder_SetStatus recompute — same shape as
   PurchaseOrderService.CreateGoodsReceiptAsync.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderReceipt_Create
(
    @InternalOrderReceiptToken UNIQUEIDENTIFIER,
    @InternalOrderId           INT,
    @Notes                     NVARCHAR(1000) = NULL,
    @CreatedBy                 VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InternalOrderReceipts
        (InternalOrderReceiptToken, InternalOrderId, Notes, CreatedBy)
    VALUES
        (@InternalOrderReceiptToken, @InternalOrderId, @Notes, @CreatedBy);

    SELECT
        ior.InternalOrderReceiptId, ior.InternalOrderReceiptToken,
        ior.InternalOrderId, io.InternalOrderToken, io.InternalOrderNumber,
        ior.Notes,
        ior.CreatedUtc, ior.CreatedBy
    FROM dbo.InternalOrderReceipts ior
    JOIN dbo.InternalOrders io ON io.InternalOrderId = ior.InternalOrderId
    WHERE ior.InternalOrderReceiptToken = @InternalOrderReceiptToken;
END;
GO
