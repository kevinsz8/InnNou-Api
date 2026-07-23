SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ConsolidatedPurchaseOrder_GetByToken
(
    @ConsolidatedPurchaseOrderToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.ConsolidatedPurchaseOrderId, c.ConsolidatedPurchaseOrderToken,
        c.SupplierId, s.Name AS SupplierName,
        c.SuperAssociateOrganizationId, org.OrganizationToken, org.Name AS SuperAssociateOrganizationName,
        c.Title, c.Notes, c.DateRangeFrom, c.DateRangeTo,
        c.CreatedUtc, c.CreatedBy
    FROM dbo.ConsolidatedPurchaseOrders c
    JOIN dbo.Suppliers s ON s.SupplierId = c.SupplierId
    JOIN dbo.Organizations org ON org.OrganizationId = c.SuperAssociateOrganizationId
    WHERE c.ConsolidatedPurchaseOrderToken = @ConsolidatedPurchaseOrderToken;
END;
GO
