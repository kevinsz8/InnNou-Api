SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPT - CREATE
   Header insert + re-select only — no transaction here, the C# service
   (PurchaseOrderService.CreateGoodsReceiptAsync) owns the transaction spanning
   this header insert, the per-line sp_GoodsReceiptLine_Create calls, and the
   sp_PurchaseOrder_SetStatus recompute, same shape as
   sp_PurchaseOrderRectification_Create.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceipt_Create
(
    @GoodsReceiptToken UNIQUEIDENTIFIER,
    @PurchaseOrderId   INT,
    @WarehouseId       INT,
    @Notes             NVARCHAR(1000) = NULL,
    @CreatedBy         VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.GoodsReceipt (GoodsReceiptToken, PurchaseOrderId, WarehouseId, Notes, CreatedBy)
    VALUES (@GoodsReceiptToken, @PurchaseOrderId, @WarehouseId, @Notes, @CreatedBy);

    SELECT
        gr.GoodsReceiptId, gr.GoodsReceiptToken,
        gr.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        gr.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        gr.Notes, gr.CreatedUtc, gr.CreatedBy
    FROM dbo.GoodsReceipt gr
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = gr.PurchaseOrderId
    JOIN dbo.Warehouses w     ON w.WarehouseId      = gr.WarehouseId
    WHERE gr.GoodsReceiptToken = @GoodsReceiptToken;
END;
GO
