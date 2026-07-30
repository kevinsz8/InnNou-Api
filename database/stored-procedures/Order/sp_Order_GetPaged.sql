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

   @FromDate/@ToDate filter on CreatedUtc (inclusive both ends — @ToDate is
   bumped a full day so a caller passing a bare date still captures every
   order created that day regardless of time-of-day), same shape as
   sp_InventoryTransfer_GetPaged's own date-range filter.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Order_GetPaged
(
    @RootOrganizationId INT          = NULL,
    @WarehouseId        INT          = NULL,
    @StatusId            INT         = NULL,
    @FromDate           DATE         = NULL,
    @ToDate             DATE         = NULL,
    @PageNumber         INT,
    @PageSize           INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ToDateExclusive DATETIME2 = CASE WHEN @ToDate IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2)) END;

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
        os.Code AS Status, o.Notes, o.SubmittedUtc, o.PdfUrl,
        o.CreatedUtc, o.CreatedBy, o.LastUpdatedUtc, o.LastUpdatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.[Order] o
    JOIN dbo.Organizations org ON org.OrganizationId = o.OrganizationId
    JOIN dbo.Warehouses    w   ON w.WarehouseId      = o.WarehouseId
    JOIN dbo.OrderStatuses os  ON os.OrderStatusId    = o.OrderStatusId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.OrderLine ol WHERE ol.OrderId = o.OrderId) lc
    WHERE
        (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = o.OrganizationId))
        AND (@WarehouseId IS NULL OR o.WarehouseId = @WarehouseId)
        AND (@StatusId IS NULL OR o.OrderStatusId = @StatusId)
        AND (@FromDate IS NULL OR o.CreatedUtc >= @FromDate)
        AND (@ToDateExclusive IS NULL OR o.CreatedUtc < @ToDateExclusive)
    ORDER BY o.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
