SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DASHBOARD - GET SPEND BY MONTH
   Sparse rows: one per (Year, Month, CurrencyCode) with spend in the last
   7 months (current month inclusive) — no currency blending, this
   codebase has no FX conversion anywhere. DashboardService fills in the
   full 7-month grid for the caller's resolved currency, defaulting
   missing months to 0. Serves both the "Spend this month" KPI (last row)
   and the chart (all 7).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetSpendByMonth
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
        pol.CurrencyCode,
        SUM(pol.Quantity * pol.UnitPrice) AS Total
    FROM dbo.PurchaseOrderLine pol
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = pol.PurchaseOrderId
    WHERE po.CreatedUtc >= @RangeStart
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId))
    GROUP BY YEAR(po.CreatedUtc), MONTH(po.CreatedUtc), pol.CurrencyCode
    ORDER BY [Year], [Month];
END;
GO
