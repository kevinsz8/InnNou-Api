SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYMOVEMENT - CREATE BATCH
   Table-valued-parameter sibling of sp_InventoryMovement_Create — inserts every movement row for
   one operation in a single round trip instead of one call per row. Generic across every
   movement origin (@Type per row, same Code->Id resolution the single-row version already does),
   not just Goods Receipts, so future batch callers (Transfers, period-close Adjustments) can
   reuse it as-is.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryMovement_CreateBatch
(
    @Movements dbo.InventoryMovementTableType READONLY,
    @CreatedBy VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InventoryMovements
        (InventoryMovementToken, WarehouseId, ArticleId, InventoryMovementTypeId, Quantity, GoodsReceiptLineId, InventoryTransferLineId, InventoryPeriodCountId, Reason, CreatedBy)
    SELECT
        m.InventoryMovementToken, m.WarehouseId, m.ArticleId, mt.InventoryMovementTypeId, m.Quantity,
        m.GoodsReceiptLineId, m.InventoryTransferLineId, m.InventoryPeriodCountId, m.Reason, @CreatedBy
    FROM @Movements m
    JOIN dbo.InventoryMovementTypes mt ON mt.Code = m.Type;
END;
GO
