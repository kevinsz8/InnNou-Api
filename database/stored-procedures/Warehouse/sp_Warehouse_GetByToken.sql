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
    WHERE w.WarehouseToken = @WarehouseToken
      AND w.IsDeleted = 0;
END;
GO
