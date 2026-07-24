SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYTRANSFER - CREATE
   Header insert + re-select only — InventoryService owns the transaction
   spanning this header insert, the per-line sp_InventoryTransferLine_Create
   + sp_StockLevel_ApplyDelta + sp_InventoryMovement_Create calls, same shape
   as sp_GoodsReceipt_Create/sp_PurchaseOrderRectification_Create.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryTransfer_Create
(
    @InventoryTransferToken UNIQUEIDENTIFIER,
    @FromWarehouseId        INT,
    @ToWarehouseId          INT,
    @Notes                  NVARCHAR(1000) = NULL,
    @CreatedBy              VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InventoryTransfers (InventoryTransferToken, FromWarehouseId, ToWarehouseId, Notes, CreatedBy)
    VALUES (@InventoryTransferToken, @FromWarehouseId, @ToWarehouseId, @Notes, @CreatedBy);

    SELECT
        it.InventoryTransferId, it.InventoryTransferToken,
        it.FromWarehouseId, fw.WarehouseToken AS FromWarehouseToken, fw.Name AS FromWarehouseName,
        it.ToWarehouseId, tw.WarehouseToken AS ToWarehouseToken, tw.Name AS ToWarehouseName,
        it.Notes, it.CreatedUtc, it.CreatedBy
    FROM dbo.InventoryTransfers it
    JOIN dbo.Warehouses fw ON fw.WarehouseId = it.FromWarehouseId
    JOIN dbo.Warehouses tw ON tw.WarehouseId = it.ToWarehouseId
    WHERE it.InventoryTransferToken = @InventoryTransferToken;
END;
GO
