SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIOD - GET PAGED
   Same hierarchy-descent CTE shape as sp_StockLevel_GetPaged/
   sp_GoodsReceipt_GetPaged. @WarehouseId optionally narrows within the
   resolved scope. LineCount mirrors sp_GoodsReceipt_GetPaged's own
   CROSS APPLY — the list is always small (bounded by warehouse count of
   periods), so eager line-count is cheap.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriod_GetPaged
(
    @RootOrganizationId INT = NULL,
    @WarehouseId        INT = NULL,
    @PageNumber         INT,
    @PageSize           INT
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationHierarchy AS
    (
        SELECT o.OrganizationId
        FROM dbo.Organizations o
        WHERE o.OrganizationId = @RootOrganizationId

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
    )
    SELECT
        ip.InventoryPeriodId, ip.InventoryPeriodToken,
        ip.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        ips.Code AS Status,
        ip.StartDate, ip.ClosedUtc, ip.ClosedBy, ip.ReopenedUtc, ip.ReopenedBy, ip.Notes,
        ip.CreatedUtc, ip.CreatedBy, ip.LastUpdatedUtc, ip.LastUpdatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.InventoryPeriods ip
    JOIN dbo.Warehouses w              ON w.WarehouseId              = ip.WarehouseId
    JOIN dbo.InventoryPeriodStatuses ips ON ips.InventoryPeriodStatusId = ip.InventoryPeriodStatusId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.InventoryPeriodCounts ipc WHERE ipc.InventoryPeriodId = ip.InventoryPeriodId) lc
    WHERE (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = w.OrganizationId))
      AND (@WarehouseId IS NULL OR ip.WarehouseId = @WarehouseId)
    ORDER BY ip.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
