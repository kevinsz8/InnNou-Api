SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIOD - GET MOST RECENT BY WAREHOUSE ID
   The single most recently created period for a warehouse, regardless of
   status — since periods for a warehouse are continuous with no gaps/
   overlaps (enforced structurally, see UX_InventoryPeriods_OneActivePerWarehouse),
   "most recently created" and "most recently closed, if none is currently
   active" are the same row. Used by InventoryPeriodService.ReopenAsync to
   reject reopening anything but the last period ever opened for that
   warehouse (INVENTORY_PERIOD_NOT_MOST_RECENT) — reopening an older one
   would silently break the continuity of everything opened after it.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriod_GetMostRecentByWarehouseId
(
    @WarehouseId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        ip.InventoryPeriodId, ip.InventoryPeriodToken,
        ip.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        ips.Code AS Status,
        ip.StartDate, ip.ClosedUtc, ip.ClosedBy, ip.ReopenedUtc, ip.ReopenedBy, ip.Notes,
        ip.CreatedUtc, ip.CreatedBy, ip.LastUpdatedUtc, ip.LastUpdatedBy
    FROM dbo.InventoryPeriods ip
    JOIN dbo.Warehouses w              ON w.WarehouseId              = ip.WarehouseId
    JOIN dbo.InventoryPeriodStatuses ips ON ips.InventoryPeriodStatusId = ip.InventoryPeriodStatusId
    WHERE ip.WarehouseId = @WarehouseId
    ORDER BY ip.InventoryPeriodId DESC;
END;
GO
