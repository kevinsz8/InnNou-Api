SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE - SET ACTIVE
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Warehouse_SetActive
(
    @WarehouseToken UNIQUEIDENTIFIER,
    @IsActive       BIT,
    @LastUpdatedUtc DATETIME2,
    @LastUpdatedBy  VARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Warehouses WHERE WarehouseToken = @WarehouseToken AND IsDeleted = 0)
    BEGIN
        RAISERROR('WAREHOUSE_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE dbo.Warehouses
    SET
        IsActive       = @IsActive,
        LastUpdatedUtc = @LastUpdatedUtc,
        LastUpdatedBy  = @LastUpdatedBy
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
