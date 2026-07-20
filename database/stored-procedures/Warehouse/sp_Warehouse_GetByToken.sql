SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Warehouse_GetByToken
(
    @WarehouseToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        WarehouseId, WarehouseToken, OrganizationId, Name, NormalizedName, Code, Description,
        IsInventoriable, CanReceivePurchases, CanReceiveTransfers, CanTransferOut,
        CanConsumeInventory, CanProduceItems, CanSellItems, CanAdjustInventory, CanReceiveReturns,
        TrackLotNumbers, TrackExpirationDates, TrackSerialNumbers, RequireApproval,
        IsDefaultReceivingWarehouse, IsDefaultConsumptionWarehouse, IsMainWarehouse,
        IsActive, IsDeleted, CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy, DeletedUtc, DeletedBy
    FROM dbo.Warehouses
    WHERE WarehouseToken = @WarehouseToken
      AND IsDeleted = 0;
END;
GO
