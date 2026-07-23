SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   CONSOLIDATEDPURCHASEORDER - CREATE
   Header only — ConsolidatedPurchaseOrderService inserts one
   ConsolidatedPurchaseOrderMember row per selected PurchaseOrder right
   after this, inside the same transaction.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ConsolidatedPurchaseOrder_Create
(
    @ConsolidatedPurchaseOrderToken UNIQUEIDENTIFIER,
    @SupplierId                     INT,
    @SuperAssociateOrganizationId   INT,
    @Title                          NVARCHAR(200) = NULL,
    @Notes                          NVARCHAR(500) = NULL,
    @DateRangeFrom                  DATE,
    @DateRangeTo                    DATE,
    @CreatedBy                      VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ConsolidatedPurchaseOrders
        (ConsolidatedPurchaseOrderToken, SupplierId, SuperAssociateOrganizationId, Title, Notes, DateRangeFrom, DateRangeTo, CreatedBy)
    VALUES
        (@ConsolidatedPurchaseOrderToken, @SupplierId, @SuperAssociateOrganizationId, @Title, @Notes, @DateRangeFrom, @DateRangeTo, @CreatedBy);

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
