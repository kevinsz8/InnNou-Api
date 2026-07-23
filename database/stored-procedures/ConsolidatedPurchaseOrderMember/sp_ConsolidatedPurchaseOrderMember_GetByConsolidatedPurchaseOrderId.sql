SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   CONSOLIDATEDPURCHASEORDERMEMBER - GET BY CONSOLIDATEDPURCHASEORDERID
   Full PurchaseOrder details per member in one query (no N+1) —
   ConsolidatedPurchaseOrderService fetches each member's own LINES
   separately via sp_PurchaseOrderLine_GetEffective only when building
   the PDF, not on every read.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ConsolidatedPurchaseOrderMember_GetByConsolidatedPurchaseOrderId
(
    @ConsolidatedPurchaseOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.ConsolidatedPurchaseOrderMemberId, m.ConsolidatedPurchaseOrderId,
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.OrderId, ord.OrderToken,
        po.SupplierId, s.Name AS SupplierName,
        po.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        po.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        pos.Code AS Status, po.SentUtc,
        lc.LineCount,
        m.CreatedUtc, m.CreatedBy
    FROM dbo.ConsolidatedPurchaseOrderMembers m
    JOIN dbo.PurchaseOrder po          ON po.PurchaseOrderId = m.PurchaseOrderId
    JOIN dbo.[Order] ord               ON ord.OrderId        = po.OrderId
    JOIN dbo.Suppliers s               ON s.SupplierId       = po.SupplierId
    JOIN dbo.Organizations org         ON org.OrganizationId = po.OrganizationId
    JOIN dbo.Warehouses w              ON w.WarehouseId      = po.WarehouseId
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.PurchaseOrderLine pol WHERE pol.PurchaseOrderId = po.PurchaseOrderId) lc
    WHERE m.ConsolidatedPurchaseOrderId = @ConsolidatedPurchaseOrderId
    ORDER BY org.Name, po.SentUtc;
END;
GO
