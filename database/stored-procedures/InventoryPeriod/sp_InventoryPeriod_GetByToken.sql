SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIOD - GET BY TOKEN
   Header only — sp_InventoryPeriodCount_GetByPeriodId populates Lines, same
   "second query, always" convention as GoodsReceiptDto.Lines.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriod_GetByToken
(
    @InventoryPeriodToken UNIQUEIDENTIFIER
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
    WHERE ip.InventoryPeriodToken = @InventoryPeriodToken;
END;
GO
