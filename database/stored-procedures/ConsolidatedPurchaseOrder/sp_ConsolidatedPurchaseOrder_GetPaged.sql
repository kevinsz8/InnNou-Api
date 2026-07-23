SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   CONSOLIDATEDPURCHASEORDER - GET PAGED
   Scoped to exactly one SUPER_ASSOCIATE organization — visibility is
   never hierarchy-wide like Orders/PurchaseOrders, since this is a
   group-level negotiation tool, not something descendant properties
   see. MemberCount via CROSS APPLY, same anti-N+1 convention as
   PurchaseOrder.LineCount.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ConsolidatedPurchaseOrder_GetPaged
(
    @SuperAssociateOrganizationId INT,
    @PageNumber                   INT,
    @PageSize                     INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.ConsolidatedPurchaseOrderId, c.ConsolidatedPurchaseOrderToken,
        c.SupplierId, s.Name AS SupplierName,
        c.SuperAssociateOrganizationId, org.OrganizationToken, org.Name AS SuperAssociateOrganizationName,
        c.Title, c.Notes, c.DateRangeFrom, c.DateRangeTo,
        c.CreatedUtc, c.CreatedBy,
        mc.MemberCount,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.ConsolidatedPurchaseOrders c
    JOIN dbo.Suppliers s ON s.SupplierId = c.SupplierId
    JOIN dbo.Organizations org ON org.OrganizationId = c.SuperAssociateOrganizationId
    CROSS APPLY (SELECT COUNT(*) AS MemberCount FROM dbo.ConsolidatedPurchaseOrderMembers m WHERE m.ConsolidatedPurchaseOrderId = c.ConsolidatedPurchaseOrderId) mc
    WHERE c.SuperAssociateOrganizationId = @SuperAssociateOrganizationId
    ORDER BY c.CreatedUtc DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
