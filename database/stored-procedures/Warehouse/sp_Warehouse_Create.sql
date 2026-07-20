SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE - CREATE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Warehouse_Create
(
    @WarehouseToken   UNIQUEIDENTIFIER,
    @OrganizationId   INT,
    @Name             VARCHAR(200),
    @NormalizedName   VARCHAR(200),
    @Code             VARCHAR(50)  = NULL,
    @Description      VARCHAR(500) = NULL,

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
    @IsMainWarehouse               BIT,

    @CreatedUtc       DATETIME2,
    @CreatedBy        VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Organizations WHERE OrganizationId = @OrganizationId AND IsDeleted = 0)
    BEGIN
        RAISERROR('WAREHOUSE_ORGANIZATION_NOT_FOUND', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Warehouses
    (
        WarehouseToken, OrganizationId, Name, NormalizedName, Code, Description,
        IsInventoriable, CanReceivePurchases, CanReceiveTransfers, CanTransferOut,
        CanConsumeInventory, CanProduceItems, CanSellItems, CanAdjustInventory, CanReceiveReturns,
        TrackLotNumbers, TrackExpirationDates, TrackSerialNumbers, RequireApproval,
        IsDefaultReceivingWarehouse, IsDefaultConsumptionWarehouse, IsMainWarehouse,
        IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    VALUES
    (
        @WarehouseToken, @OrganizationId, @Name, @NormalizedName, @Code, @Description,
        @IsInventoriable, @CanReceivePurchases, @CanReceiveTransfers, @CanTransferOut,
        @CanConsumeInventory, @CanProduceItems, @CanSellItems, @CanAdjustInventory, @CanReceiveReturns,
        @TrackLotNumbers, @TrackExpirationDates, @TrackSerialNumbers, @RequireApproval,
        @IsDefaultReceivingWarehouse, @IsDefaultConsumptionWarehouse, @IsMainWarehouse,
        1, 0, @CreatedUtc, @CreatedBy
    );

    SELECT
        WarehouseId, WarehouseToken, OrganizationId, Name, NormalizedName, Code, Description,
        IsInventoriable, CanReceivePurchases, CanReceiveTransfers, CanTransferOut,
        CanConsumeInventory, CanProduceItems, CanSellItems, CanAdjustInventory, CanReceiveReturns,
        TrackLotNumbers, TrackExpirationDates, TrackSerialNumbers, RequireApproval,
        IsDefaultReceivingWarehouse, IsDefaultConsumptionWarehouse, IsMainWarehouse,
        IsActive, IsDeleted, CreatedUtc, CreatedBy, LastUpdatedUtc, LastUpdatedBy, DeletedUtc, DeletedBy
    FROM dbo.Warehouses
    WHERE WarehouseToken = @WarehouseToken;
END;
GO
