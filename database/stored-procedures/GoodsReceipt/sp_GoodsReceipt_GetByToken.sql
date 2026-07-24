SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPT - GET BY TOKEN
   Header only — sp_GoodsReceiptLine_GetByGoodsReceiptId populates Lines,
   same "second query, always" convention as OrderDto.Lines.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceipt_GetByToken
(
    @GoodsReceiptToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        gr.GoodsReceiptId, gr.GoodsReceiptToken,
        gr.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.SupplierId, po.OrganizationId,
        gr.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        gr.Notes, gr.CreatedUtc, gr.CreatedBy
    FROM dbo.GoodsReceipt gr
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = gr.PurchaseOrderId
    JOIN dbo.Warehouses w     ON w.WarehouseId      = gr.WarehouseId
    WHERE gr.GoodsReceiptToken = @GoodsReceiptToken;
END;
GO
