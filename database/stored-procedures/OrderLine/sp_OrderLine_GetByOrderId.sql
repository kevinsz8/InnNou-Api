SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERLINE - GET BY ORDER ID
   Denormalizes Article/Supplier/Unit names so the caller has a
   directly-readable list. Also what OrderService.SubmitAsync uses
   to resolve each line's SupplierId for the per-supplier split.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderLine_GetByOrderId
(
    @OrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ol.OrderLineId, ol.OrderLineToken, ol.OrderId, ord.OrderToken,
        ol.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName,
        ol.Quantity,
        ol.PurchaseUnitId, pu.Code AS PurchaseUnitCode,
        ol.PurchaseQuantity,
        ol.ContentUnitId, cu.Code AS ContentUnitCode,
        ol.ContentQuantity,
        ol.UnitPrice, ol.CurrencyCode,
        ol.CategoryId, ol.CategoryCode, ol.SubCategoryId, ol.SubCategoryCode,
        ol.Notes,
        ol.CreatedUtc, ol.CreatedBy, ol.LastUpdatedUtc, ol.LastUpdatedBy
    FROM dbo.OrderLine ol
    JOIN dbo.[Order] ord       ON ord.OrderId        = ol.OrderId
    JOIN dbo.Articles a        ON a.ArticleId        = ol.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId       = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu ON pu.UnitOfMeasureId = ol.PurchaseUnitId
    JOIN dbo.UnitsOfMeasure cu ON cu.UnitOfMeasureId = ol.ContentUnitId
    WHERE ol.OrderId = @OrderId
    ORDER BY ol.OrderLineId;
END;
GO
