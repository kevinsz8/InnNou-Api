SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - GET PAGED
   Two visibility branches, same shape as ArticleService's
   supplier-vs-organization catalog rule: @SupplierId set scopes to
   the owning supplier's own purchase orders; otherwise the
   organization-hierarchy branch applies (@RootOrganizationId = NULL
   is unrestricted, SuperAdmin only).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_GetPaged
(
    @RootOrganizationId INT          = NULL,
    @SupplierId         INT          = NULL,
    @OrderId            INT          = NULL,
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
        po.PurchaseOrderId, po.PurchaseOrderToken,
        po.OrderId, ord.OrderToken,
        po.SupplierId, s.Name AS SupplierName,
        po.OrganizationId, org.OrganizationToken,
        po.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        po.Status, po.SentUtc, po.CancelledUtc, po.CancelledBy,
        po.CreatedUtc, po.CreatedBy,
        lc.LineCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.PurchaseOrder po
    JOIN dbo.[Order] ord        ON ord.OrderId        = po.OrderId
    JOIN dbo.Suppliers s        ON s.SupplierId       = po.SupplierId
    JOIN dbo.Organizations org  ON org.OrganizationId = po.OrganizationId
    JOIN dbo.Warehouses w       ON w.WarehouseId      = po.WarehouseId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.PurchaseOrderLine pol WHERE pol.PurchaseOrderId = po.PurchaseOrderId) lc
    WHERE
        (
            (@SupplierId IS NOT NULL AND po.SupplierId = @SupplierId)
            OR (@SupplierId IS NULL AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId)))
        )
        AND (@OrderId IS NULL OR po.OrderId = @OrderId)
        AND (@Status IS NULL OR po.Status = @Status)
    ORDER BY po.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
