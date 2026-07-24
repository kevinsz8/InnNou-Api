SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYMOVEMENT - GET PAGED
   History for one Warehouse (required), optionally narrowed to a single
   Article — the "why is stock what it is" drill-down behind a StockLevel
   row. Caller-side org-hierarchy access to @WarehouseId is validated by
   InventoryService before calling this (same shape as
   sp_GoodsReceiptLine_GetByPurchaseOrderId's own caller-validates-first
   convention).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryMovement_GetPaged
(
    @WarehouseId INT,
    @ArticleId   INT = NULL,
    @PageNumber  INT,
    @PageSize    INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        im.InventoryMovementId, im.InventoryMovementToken,
        im.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        im.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        mt.Code AS Type, im.Quantity,
        gr.GoodsReceiptToken, it.InventoryTransferToken,
        im.Reason, im.CreatedUtc, im.CreatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.InventoryMovements im
    JOIN dbo.Warehouses w                          ON w.WarehouseId              = im.WarehouseId
    JOIN dbo.Articles a                            ON a.ArticleId                = im.ArticleId
    JOIN dbo.InventoryMovementTypes mt              ON mt.InventoryMovementTypeId = im.InventoryMovementTypeId
    LEFT JOIN dbo.GoodsReceiptLine grl              ON grl.GoodsReceiptLineId     = im.GoodsReceiptLineId
    LEFT JOIN dbo.GoodsReceipt gr                   ON gr.GoodsReceiptId          = grl.GoodsReceiptId
    LEFT JOIN dbo.InventoryTransferLines tl         ON tl.InventoryTransferLineId = im.InventoryTransferLineId
    LEFT JOIN dbo.InventoryTransfers it              ON it.InventoryTransferId     = tl.InventoryTransferId
    WHERE im.WarehouseId = @WarehouseId
      AND (@ArticleId IS NULL OR im.ArticleId = @ArticleId)
    ORDER BY im.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
