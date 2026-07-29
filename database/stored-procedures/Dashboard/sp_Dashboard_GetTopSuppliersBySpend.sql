SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DASHBOARD - GET TOP SUPPLIERS BY SPEND
   Sparse rows: one per (SupplierId, CurrencyCode) with spend in the
   current calendar month — no currency blending, same reasoning as
   sp_Dashboard_GetSpendByMonth. Returns every supplier with spend
   this month, not just the top 5: currency filtering happens in
   DashboardService (against the same resolved currency as the spend
   KPI/chart) before the top-5 trim, so a supplier billed in a
   currency other than the reported one is never able to bump a
   same-currency supplier out of the list.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetTopSuppliersBySpend
(
    @RootOrganizationId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RangeStart DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);

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
        s.SupplierId,
        s.SupplierToken,
        s.Name AS SupplierName,
        pol.CurrencyCode,
        SUM(pol.Quantity * pol.UnitPrice) AS Total
    FROM dbo.PurchaseOrderLine pol
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = pol.PurchaseOrderId
    JOIN dbo.Suppliers s      ON s.SupplierId       = po.SupplierId
    WHERE po.CreatedUtc >= @RangeStart
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId))
    GROUP BY s.SupplierId, s.SupplierToken, s.Name, pol.CurrencyCode
    ORDER BY Total DESC;
END;
GO
