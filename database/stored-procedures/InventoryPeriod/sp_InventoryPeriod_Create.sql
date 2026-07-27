SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIOD - CREATE
   Header insert + re-select only — no transaction here, the C# service
   (InventoryPeriodService.OpenAsync) owns the transaction spanning this
   header insert and the per-article sp_InventoryPeriodCount_Create calls,
   same shape as sp_GoodsReceipt_Create. Always inserted at StatusId=1
   (OPEN) via the column default.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriod_Create
(
    @InventoryPeriodToken UNIQUEIDENTIFIER,
    @WarehouseId          INT,
    @StartDate            DATETIME2,
    @Notes                NVARCHAR(1000) = NULL,
    @CreatedBy            VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InventoryPeriods (InventoryPeriodToken, WarehouseId, StartDate, Notes, CreatedBy)
    VALUES (@InventoryPeriodToken, @WarehouseId, @StartDate, @Notes, @CreatedBy);

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
