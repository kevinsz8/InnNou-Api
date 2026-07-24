SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYMOVEMENT - CREATE
   Append-only audit ledger row — never updated/deleted. @Type resolved to
   Id via the same inline-subquery pattern as every other Id-backed status/
   type column. Exactly one of @GoodsReceiptLineId/@InventoryTransferLineId
   is set depending on @Type (RECEIPT / TRANSFER_OUT+TRANSFER_IN); neither
   is set for ADJUSTMENT.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryMovement_Create
(
    @InventoryMovementToken  UNIQUEIDENTIFIER,
    @WarehouseId             INT,
    @ArticleId               INT,
    @Type                    VARCHAR(20),
    @Quantity                DECIMAL(18,4),
    @GoodsReceiptLineId      INT           = NULL,
    @InventoryTransferLineId INT           = NULL,
    @Reason                  NVARCHAR(500) = NULL,
    @CreatedBy               VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InventoryMovements
        (InventoryMovementToken, WarehouseId, ArticleId, InventoryMovementTypeId, Quantity, GoodsReceiptLineId, InventoryTransferLineId, Reason, CreatedBy)
    VALUES
        (@InventoryMovementToken, @WarehouseId, @ArticleId,
         (SELECT InventoryMovementTypeId FROM dbo.InventoryMovementTypes WHERE Code = @Type),
         @Quantity, @GoodsReceiptLineId, @InventoryTransferLineId, @Reason, @CreatedBy);

    SELECT
        im.InventoryMovementId, im.InventoryMovementToken,
        im.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        im.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        mt.Code AS Type, im.Quantity,
        gr.GoodsReceiptToken, it.InventoryTransferToken,
        im.Reason, im.CreatedUtc, im.CreatedBy
    FROM dbo.InventoryMovements im
    JOIN dbo.Warehouses w                          ON w.WarehouseId              = im.WarehouseId
    JOIN dbo.Articles a                            ON a.ArticleId                = im.ArticleId
    JOIN dbo.InventoryMovementTypes mt              ON mt.InventoryMovementTypeId = im.InventoryMovementTypeId
    LEFT JOIN dbo.GoodsReceiptLine grl              ON grl.GoodsReceiptLineId     = im.GoodsReceiptLineId
    LEFT JOIN dbo.GoodsReceipt gr                   ON gr.GoodsReceiptId          = grl.GoodsReceiptId
    LEFT JOIN dbo.InventoryTransferLines tl         ON tl.InventoryTransferLineId = im.InventoryTransferLineId
    LEFT JOIN dbo.InventoryTransfers it              ON it.InventoryTransferId     = tl.InventoryTransferId
    WHERE im.InventoryMovementToken = @InventoryMovementToken;
END;
GO
