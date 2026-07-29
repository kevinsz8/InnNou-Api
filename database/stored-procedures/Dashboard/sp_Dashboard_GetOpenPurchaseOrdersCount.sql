SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DASHBOARD - GET OPEN PURCHASE ORDERS COUNT
   Count of PurchaseOrder rows currently sitting in SENT or
   PARTIALLY_RECEIVED — "what do I have in transit right now that
   still needs a Goods Receipt against it", a snapshot of the current
   state rather than a month-bucketed history. Replaces the earlier
   order-count-by-month-by-status chart, which answered a more
   confusing question (see that SP's removal migration).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetOpenPurchaseOrdersCount
(
    @RootOrganizationId INT = NULL
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
    SELECT COUNT(*) AS OpenPurchaseOrdersCount
    FROM dbo.PurchaseOrder po
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    WHERE pos.Code IN ('SENT', 'PARTIALLY_RECEIVED')
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId));
END;
GO
