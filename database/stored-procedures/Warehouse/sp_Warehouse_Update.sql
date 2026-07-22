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
    @Description      VARCHAR(MAX) = NULL,

    @AddressLine1     VARCHAR(250) = NULL,
    @AddressLine2     VARCHAR(250) = NULL,
    @City             VARCHAR(150) = NULL,
    @State            VARCHAR(150) = NULL,
    @PostalCode       VARCHAR(50)  = NULL,
    @Country          VARCHAR(100) = NULL,
    @ZoneId           INT          = NULL,

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
        AddressLine1                  = @AddressLine1,
        AddressLine2                  = @AddressLine2,
        City                          = @City,
        State                         = @State,
        PostalCode                    = @PostalCode,
        Country                       = @Country,
        ZoneId                        = @ZoneId,
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
        IsMainWarehouse               = @IsMainWarehouse,
        LastUpdatedUtc                = @LastUpdatedUtc,
        LastUpdatedBy                 = @LastUpdatedBy
    WHERE WarehouseToken = @WarehouseToken
      AND IsDeleted = 0;

    SELECT
        w.WarehouseId, w.WarehouseToken, w.OrganizationId, w.Name, w.NormalizedName, w.Code, w.Description,
        w.AddressLine1, w.AddressLine2, w.City, w.State, w.PostalCode, w.Country,
        w.ZoneId, z.ZoneToken, z.Code AS ZoneCode, z.Name AS ZoneName, zc.Code AS CountryCode, zc.Name AS CountryName,
        w.IsInventoriable, w.CanReceivePurchases, w.CanReceiveTransfers, w.CanTransferOut,
        w.CanConsumeInventory, w.CanProduceItems, w.CanSellItems, w.CanAdjustInventory, w.CanReceiveReturns,
        w.TrackLotNumbers, w.TrackExpirationDates, w.TrackSerialNumbers, w.RequireApproval,
        w.IsDefaultReceivingWarehouse, w.IsDefaultConsumptionWarehouse, w.IsMainWarehouse,
        w.IsActive, w.IsDeleted, w.CreatedUtc, w.CreatedBy, w.LastUpdatedUtc, w.LastUpdatedBy, w.DeletedUtc, w.DeletedBy
    FROM dbo.Warehouses w
    LEFT JOIN dbo.Zones z      ON z.ZoneId = w.ZoneId
    LEFT JOIN dbo.Countries zc ON zc.CountryId = z.CountryId
    WHERE w.WarehouseToken = @WarehouseToken;
END;
GO
