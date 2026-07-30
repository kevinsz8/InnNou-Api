SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERRETURN - GET PAGED
   Buyer-side only (no supplier read access, confirmed with the user) —
   unlike sp_PurchaseOrder_GetPaged's own @SupplierId, this is NEVER the
   caller's own identity bypassing the hierarchy check, only an optional
   narrowing filter layered on top of it (@RootOrganizationId = NULL is
   unrestricted, SuperAdmin only) — same additive-AND-on-top-of-scope shape
   the RoleIds/OrganizationIds multi-value filter convention established,
   never an OR that could widen a caller's own visibility.
   @FromDate/@ToDate filter on CreatedUtc, same inclusive-both-ends shape
   as every other date-range filter here. @PurchaseOrderNumber is a plain
   contains-match (same LOWER()+LIKE shape as sp_Supplier_GetPaged's own
   text search) — lets a caller jump straight to "is there already a
   return against PO-2026-00012" instead of paging through the whole list.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierReturn_GetPaged
(
    @RootOrganizationId  INT          = NULL,
    @SupplierId          INT          = NULL,
    @StatusId            INT          = NULL,
    @FromDate            DATE         = NULL,
    @ToDate              DATE         = NULL,
    @PurchaseOrderNumber VARCHAR(20)  = NULL,
    @PageNumber          INT,
    @PageSize            INT
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
        r.SupplierReturnId, r.SupplierReturnToken,
        r.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        po.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        statuses.Code AS Status,
        resolutionTypes.Code AS ResolutionType,
        r.Notes, r.ClosedUtc, r.ClosedBy, r.CreatedUtc, r.CreatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.SupplierReturns r
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = r.PurchaseOrderId
    JOIN dbo.Organizations org ON org.OrganizationId = po.OrganizationId
    JOIN dbo.Suppliers s ON s.SupplierId = po.SupplierId
    JOIN dbo.SupplierReturnStatuses statuses ON statuses.SupplierReturnStatusId = r.SupplierReturnStatusId
    LEFT JOIN dbo.SupplierReturnResolutionTypes resolutionTypes ON resolutionTypes.SupplierReturnResolutionTypeId = r.SupplierReturnResolutionTypeId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.SupplierReturnLines srl WHERE srl.SupplierReturnId = r.SupplierReturnId) lc
    WHERE
        (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId))
        AND (@SupplierId IS NULL OR po.SupplierId = @SupplierId)
        AND (@StatusId IS NULL OR r.SupplierReturnStatusId = @StatusId)
        AND (@FromDate IS NULL OR r.CreatedUtc >= @FromDate)
        AND (@ToDateExclusive IS NULL OR r.CreatedUtc < @ToDateExclusive)
        AND (@PurchaseOrderNumber IS NULL OR LOWER(po.PurchaseOrderNumber) LIKE '%' + LOWER(@PurchaseOrderNumber) + '%')
    ORDER BY r.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
