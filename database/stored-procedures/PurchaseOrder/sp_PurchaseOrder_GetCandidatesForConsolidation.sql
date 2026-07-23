SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - GET CANDIDATES FOR CONSOLIDATION
   Finds SENT PurchaseOrders from any descendant ASSOCIATE property of
   @SuperAssociateOrganizationId, for a given Supplier and date range,
   that aren't already claimed by an existing ConsolidatedPurchaseOrder
   — same hierarchy-descent CTE shape as sp_Organization_GetByToken/
   sp_PurchaseOrder_GetPaged. Only SENT orders are real commitments
   worth negotiating over; CANCELLED ones are excluded.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_GetCandidatesForConsolidation
(
    @SupplierId                   INT,
    @SuperAssociateOrganizationId INT,
    @DateFrom                     DATE,
    @DateTo                       DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationHierarchy AS
    (
        SELECT o.OrganizationId
        FROM dbo.Organizations o
        WHERE o.OrganizationId = @SuperAssociateOrganizationId

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
    )
    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.OrderId, ord.OrderToken,
        po.SupplierId, s.Name AS SupplierName,
        po.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        po.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        pos.Code AS Status, po.SentUtc, po.CancelledUtc, po.CancelledBy,
        po.CreatedUtc, po.CreatedBy,
        lc.LineCount
    FROM dbo.PurchaseOrder po
    JOIN dbo.[Order] ord              ON ord.OrderId        = po.OrderId
    JOIN dbo.Suppliers s              ON s.SupplierId       = po.SupplierId
    JOIN dbo.Organizations org        ON org.OrganizationId = po.OrganizationId
    JOIN dbo.Warehouses w             ON w.WarehouseId      = po.WarehouseId
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.PurchaseOrderLine pol WHERE pol.PurchaseOrderId = po.PurchaseOrderId) lc
    WHERE po.SupplierId = @SupplierId
      AND pos.Code = 'SENT'
      AND EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = po.OrganizationId)
      AND CAST(po.SentUtc AS DATE) BETWEEN @DateFrom AND @DateTo
      AND NOT EXISTS (SELECT 1 FROM dbo.ConsolidatedPurchaseOrderMembers m WHERE m.PurchaseOrderId = po.PurchaseOrderId)
    ORDER BY org.Name, po.SentUtc;
END;
GO
