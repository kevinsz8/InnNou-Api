SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYTRANSFER - GET PAGED
   Same hierarchy-descent CTE shape as sp_PurchaseOrder_GetPaged, scoped by
   the FromWarehouse's Organization (both warehouses always share the same
   Organization — enforced at create time, see InventoryService.CreateTransferAsync).
   @WarehouseId optionally narrows to transfers where it's either side.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryTransfer_GetPaged
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
        it.InventoryTransferId, it.InventoryTransferToken,
        it.FromWarehouseId, fw.WarehouseToken AS FromWarehouseToken, fw.Name AS FromWarehouseName,
        it.ToWarehouseId, tw.WarehouseToken AS ToWarehouseToken, tw.Name AS ToWarehouseName,
        it.Notes, it.CreatedUtc, it.CreatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.InventoryTransfers it
    JOIN dbo.Warehouses fw ON fw.WarehouseId = it.FromWarehouseId
    JOIN dbo.Warehouses tw ON tw.WarehouseId = it.ToWarehouseId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.InventoryTransferLines l WHERE l.InventoryTransferId = it.InventoryTransferId) lc
    WHERE (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = fw.OrganizationId))
      AND (@WarehouseId IS NULL OR it.FromWarehouseId = @WarehouseId OR it.ToWarehouseId = @WarehouseId)
    ORDER BY it.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
