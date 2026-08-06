SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYMOVEMENT - CREATE
   Append-only audit ledger row — never updated/deleted. @Type resolved to
   Id via the same inline-subquery pattern as every other Id-backed status/
   type column. Exactly one of @GoodsReceiptLineId/@InventoryTransferLineId/
   @InventoryPeriodCountId/@InternalOrderShipmentLineId/
   @InternalOrderReceiptLineId/@RequisitionIssueLineId is set depending on
   @Type/origin (RECEIPT / TRANSFER_OUT+TRANSFER_IN / a period close-or-
   reopen ADJUSTMENT / INTERNAL_ORDER_OUT / INTERNAL_ORDER_IN / CONSUMPTION);
   none are set for a plain manual ADJUSTMENT.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryMovement_Create
(
    @InventoryMovementToken     UNIQUEIDENTIFIER,
    @WarehouseId                INT,
    @ArticleId                  INT,
    @Type                       VARCHAR(20),
    @Quantity                   DECIMAL(18,8),
    @GoodsReceiptLineId         INT           = NULL,
    @InventoryTransferLineId    INT           = NULL,
    @InventoryPeriodCountId     INT           = NULL,
    @InternalOrderShipmentLineId INT          = NULL,
    @InternalOrderReceiptLineId  INT          = NULL,
    @RequisitionIssueLineId      INT          = NULL,
    @EnteredUnitId               INT           = NULL,
    @EnteredQuantity             DECIMAL(18,8) = NULL,
    @Reason                     NVARCHAR(500) = NULL,
    @CreatedBy                  VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InventoryMovements
        (InventoryMovementToken, WarehouseId, ArticleId, InventoryMovementTypeId, Quantity,
         GoodsReceiptLineId, InventoryTransferLineId, InventoryPeriodCountId,
         InternalOrderShipmentLineId, InternalOrderReceiptLineId, RequisitionIssueLineId,
         EnteredUnitId, EnteredQuantity, Reason, CreatedBy)
    VALUES
        (@InventoryMovementToken, @WarehouseId, @ArticleId,
         (SELECT InventoryMovementTypeId FROM dbo.InventoryMovementTypes WHERE Code = @Type),
         @Quantity, @GoodsReceiptLineId, @InventoryTransferLineId, @InventoryPeriodCountId,
         @InternalOrderShipmentLineId, @InternalOrderReceiptLineId, @RequisitionIssueLineId,
         @EnteredUnitId, @EnteredQuantity, @Reason, @CreatedBy);

    SELECT
        im.InventoryMovementId, im.InventoryMovementToken,
        im.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        im.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        mt.Code AS Type, im.Quantity,
        im.EnteredUnitId, eu.Code AS EnteredUnitCode, im.EnteredQuantity,
        gr.GoodsReceiptToken, it.InventoryTransferToken, ipc.InventoryPeriodCountToken,
        ios.InternalOrderShipmentToken, ior.InternalOrderReceiptToken, reqi.RequisitionIssueToken,
        im.Reason, im.CreatedUtc, im.CreatedBy
    FROM dbo.InventoryMovements im
    JOIN dbo.Warehouses w                          ON w.WarehouseId              = im.WarehouseId
    JOIN dbo.Articles a                            ON a.ArticleId                = im.ArticleId
    JOIN dbo.InventoryMovementTypes mt              ON mt.InventoryMovementTypeId = im.InventoryMovementTypeId
    LEFT JOIN dbo.UnitsOfMeasure eu                 ON eu.UnitOfMeasureId         = im.EnteredUnitId
    LEFT JOIN dbo.GoodsReceiptLine grl              ON grl.GoodsReceiptLineId     = im.GoodsReceiptLineId
    LEFT JOIN dbo.GoodsReceipt gr                   ON gr.GoodsReceiptId          = grl.GoodsReceiptId
    LEFT JOIN dbo.InventoryTransferLines tl         ON tl.InventoryTransferLineId = im.InventoryTransferLineId
    LEFT JOIN dbo.InventoryTransfers it              ON it.InventoryTransferId     = tl.InventoryTransferId
    LEFT JOIN dbo.InventoryPeriodCounts ipc         ON ipc.InventoryPeriodCountId = im.InventoryPeriodCountId
    LEFT JOIN dbo.InternalOrderShipmentLines iosl   ON iosl.InternalOrderShipmentLineId = im.InternalOrderShipmentLineId
    LEFT JOIN dbo.InternalOrderShipments ios        ON ios.InternalOrderShipmentId      = iosl.InternalOrderShipmentId
    LEFT JOIN dbo.InternalOrderReceiptLines iorl    ON iorl.InternalOrderReceiptLineId  = im.InternalOrderReceiptLineId
    LEFT JOIN dbo.InternalOrderReceipts ior         ON ior.InternalOrderReceiptId       = iorl.InternalOrderReceiptId
    LEFT JOIN dbo.RequisitionIssueLines reqil       ON reqil.RequisitionIssueLineId     = im.RequisitionIssueLineId
    LEFT JOIN dbo.RequisitionIssues reqi            ON reqi.RequisitionIssueId          = reqil.RequisitionIssueId
    WHERE im.InventoryMovementToken = @InventoryMovementToken;
END;
GO
