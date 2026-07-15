SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE - UPDATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Warehouse_Update
(
    @WarehouseToken   UNIQUEIDENTIFIER,
    @Name             VARCHAR(200),
    @NormalizedName   VARCHAR(200),
    @Code             VARCHAR(50)  = NULL,
    @Description      VARCHAR(500) = NULL,
    @PurposeCode      VARCHAR(30),

    @IsInventoriable               BIT,
    @CanReceivePurchases           BIT,
    @CanReceiveTransfers           BIT,
    @CanTransferOut                BIT,
    @CanConsumeInventory           BIT,
    @CanProduceItems               BIT,
    @CanSellItems                  BIT,
    @CanAdjustInventory            BIT,
    @CanReceiveReturns             BIT,
    @TrackLotNumbers               BIT,
    @TrackExpirationDates          BIT,
    @TrackSerialNumbers            BIT,
    @RequireApproval               BIT,
    @IsDefaultReceivingWarehouse   BIT,
    @IsDefaultConsumptionWarehouse BIT,

    @LastUpdatedUtc   DATETIME2,
    @LastUpdatedBy    VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Warehouses
    SET
        Name                          = @Name,
        NormalizedName                = @NormalizedName,
        Code                          = @Code,
        Description                   = @Description,
        PurposeCode                   = @PurposeCode,
        IsInventoriable               = @IsInventoriable,
        CanReceivePurchases           = @CanReceivePurchases,
        CanReceiveTransfers           = @CanReceiveTransfers,
        CanTransferOut                = @CanTransferOut,
        CanConsumeInventory           = @CanConsumeInventory,
        CanProduceItems               = @CanProduceItems,
        CanSellItems                  = @CanSellItems,
        CanAdjustInventory            = @CanAdjustInventory,
        CanReceiveReturns             = @CanReceiveReturns,
        TrackLotNumbers               = @TrackLotNumbers,
        TrackExpirationDates          = @TrackExpirationDates,
        TrackSerialNumbers            = @TrackSerialNumbers,
        RequireApproval               = @RequireApproval,
        IsDefaultReceivingWarehouse   = @IsDefaultReceivingWarehouse,
        IsDefaultConsumptionWarehouse = @IsDefaultConsumptionWarehouse,
        LastUpdatedUtc                = @LastUpdatedUtc,
        LastUpdatedBy                 = @LastUpdatedBy
    WHERE WarehouseToken = @WarehouseToken
      AND IsDeleted = 0;

    SELECT
        WarehouseId, WarehouseToken, OrganizationId, Name, NormalizedName, Code, Description, PurposeCode,
        IsInventoriable, CanReceivePurchases, CanReceiveTransfers, CanTransferOut,
        CanConsumeInventory, CanProduceItems, CanSellItems, CanAdjustInventory, CanReceiveReturns,
        TrackLotNumbers, TrackExpirationDates, TrackSerialNumbers, RequireApproval,
        IsDefaultReceivingWarehouse, IsDefaultConsumptionWarehouse,
        IsActive, IsDeleted, CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy, DeletedUtc, DeletedBy
    FROM dbo.Warehouses
    WHERE WarehouseToken = @WarehouseToken;
END;
GO
