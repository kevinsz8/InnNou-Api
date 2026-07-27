SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DASHBOARD - GET ORDER COUNT BY MONTH BY STATUS
   Sparse rows: one per (Year, Month, StatusCode) with at least one
   PurchaseOrder in the last 7 months (current month inclusive). Buckets by
   PurchaseOrder.CreatedUtc and counts by its CURRENT PurchaseOrderStatusId —
   there is no per-status transition timestamp in the schema (e.g. no
   "ReceivedUtc" column), so this is "how many POs created in month X
   currently sit in status Y", not a status-transition timeline.
   DashboardService fills in the full 7-month x 4-status grid, defaulting
   missing combinations to 0. Same hierarchy-scoping/range-window shape as
   sp_Dashboard_GetSpendByMonth — deliberately not shared/reused, see this
   module's own "isolated read-only module" convention.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetOrderCountByMonthByStatus
(
    @RootOrganizationId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RangeStart DATE = DATEFROMPARTS(YEAR(DATEADD(MONTH, -6, SYSUTCDATETIME())), MONTH(DATEADD(MONTH, -6, SYSUTCDATETIME())), 1);

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
        YEAR(po.CreatedUtc) AS [Year],
        MONTH(po.CreatedUtc) AS [Month],
        pos.Code AS StatusCode,
        COUNT(*) AS OrderCount
    FROM dbo.PurchaseOrder po
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    WHERE po.CreatedUtc >= @RangeStart
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId))
    GROUP BY YEAR(po.CreatedUtc), MONTH(po.CreatedUtc), pos.Code
    ORDER BY [Year], [Month];
END;
GO
