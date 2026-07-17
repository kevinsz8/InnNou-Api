SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDER - GET PAGED
   @RootOrganizationId = NULL is unrestricted (SuperAdmin only) — the
   service always resolves a concrete organization for every other
   caller, same convention as sp_Organization_GetPaged /
   sp_Warehouse_GetPagedByOrganizationId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Order_GetPaged
(
    @RootOrganizationId INT          = NULL,
    @WarehouseId        INT          = NULL,
    @Status             VARCHAR(20)  = NULL,
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
        o.OrderId, o.OrderToken, o.OrganizationId, org.OrganizationToken,
        o.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        o.Status, o.Notes, o.SubmittedUtc,
        o.CreatedUtc, o.CreatedBy, o.LastUpdatedUtc, o.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.[Order] o
    JOIN dbo.Organizations org ON org.OrganizationId = o.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = o.WarehouseId
    WHERE
        (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = o.OrganizationId))
        AND (@WarehouseId IS NULL OR o.WarehouseId = @WarehouseId)
        AND (@Status IS NULL OR o.Status = @Status)
    ORDER BY o.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
