SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDERLINE - GET BY PURCHASE ORDER ID
   Used by PurchaseOrderService to compose a PurchaseOrder's own line
   list — a direct query, no filtering-in-C# over the parent Order's
   full line set needed (unlike the pre-split design).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrderLine_GetByPurchaseOrderId
(
    @PurchaseOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pol.PurchaseOrderLineId, pol.PurchaseOrderLineToken,
        pol.PurchaseOrderId, po.PurchaseOrderToken,
        pol.OrderLineId, ol.OrderLineToken,
        pol.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName,
        pol.Quantity,
        pol.PurchaseUnitId, pu.Code AS PurchaseUnitCode,
        pol.PurchaseQuantity,
        pol.ContentUnitId, cu.Code AS ContentUnitCode,
        pol.ContentQuantity,
        pol.UnitPrice, pol.CurrencyCode,
        pol.CategoryId, pol.CategoryCode, pol.SubCategoryId, pol.SubCategoryCode,
        pol.Notes,
        pol.CreatedUtc, pol.CreatedBy, pol.LastUpdatedUtc, pol.LastUpdatedBy
    FROM dbo.PurchaseOrderLine pol
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = pol.PurchaseOrderId
    JOIN dbo.OrderLine ol      ON ol.OrderLineId      = pol.OrderLineId
    JOIN dbo.Articles a        ON a.ArticleId         = pol.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId        = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu ON pu.UnitOfMeasureId  = pol.PurchaseUnitId
    JOIN dbo.UnitsOfMeasure cu ON cu.UnitOfMeasureId  = pol.ContentUnitId
    WHERE pol.PurchaseOrderId = @PurchaseOrderId
    ORDER BY pol.PurchaseOrderLineId;
END;
GO
