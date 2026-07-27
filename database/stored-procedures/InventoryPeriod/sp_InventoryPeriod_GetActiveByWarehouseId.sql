SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIOD - GET ACTIVE BY WAREHOUSE ID
   Returns the warehouse's current non-CLOSED period, if any — the filtered
   unique index UX_InventoryPeriods_OneActivePerWarehouse guarantees at most
   one row. Used both by InventoryPeriodService.OpenAsync's "already open"
   guard and by InventoryService's freeze check on Adjustments/Transfers
   (rejects with INVENTORY_WAREHOUSE_COUNT_IN_PROGRESS when the result's
   Status is IN_PROGRESS or PRE_CLOSED).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriod_GetActiveByWarehouseId
(
    @WarehouseId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ip.InventoryPeriodId, ip.InventoryPeriodToken,
        ip.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        ips.Code AS Status,
        ip.StartDate, ip.ClosedUtc, ip.ClosedBy, ip.ReopenedUtc, ip.ReopenedBy, ip.Notes,
        ip.CreatedUtc, ip.CreatedBy, ip.LastUpdatedUtc, ip.LastUpdatedBy
    FROM dbo.InventoryPeriods ip
    JOIN dbo.Warehouses w              ON w.WarehouseId              = ip.WarehouseId
    JOIN dbo.InventoryPeriodStatuses ips ON ips.InventoryPeriodStatusId = ip.InventoryPeriodStatusId
    WHERE ip.WarehouseId = @WarehouseId AND ips.Code <> 'CLOSED';
END;
GO
