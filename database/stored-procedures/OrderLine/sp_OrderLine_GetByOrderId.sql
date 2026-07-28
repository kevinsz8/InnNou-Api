SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERLINE - GET BY ORDER ID
   Denormalizes Article/Supplier/Unit names so the caller has a
   directly-readable list. Also what OrderService.SubmitAsync uses
   to resolve each line's SupplierId for the per-supplier split.

   FamilyCode/SubFamilyCode are resolved live from the Article's
   CURRENT Family/SubFamily (unlike CategoryCode/SubCategoryCode
   above, which are a frozen snapshot stored on the row itself) —
   this only backs in-page line search/filter, not historical
   reporting, so there's no "historical BI protection" need to
   freeze it. See InnNou-Web OrderLine type for the same note.
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
        f.Code AS FamilyCode, sf.Code AS SubFamilyCode,
        ol.Notes,
        ol.CreatedUtc, ol.CreatedBy, ol.LastUpdatedUtc, ol.LastUpdatedBy
    FROM dbo.OrderLine ol
    JOIN dbo.[Order] ord            ON ord.OrderId        = ol.OrderId
    JOIN dbo.Articles a             ON a.ArticleId        = ol.ArticleId
    JOIN dbo.Suppliers s            ON s.SupplierId       = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu      ON pu.UnitOfMeasureId = ol.PurchaseUnitId
    JOIN dbo.UnitsOfMeasure cu      ON cu.UnitOfMeasureId = ol.ContentUnitId
    LEFT JOIN dbo.Families f        ON f.FamilyId         = a.FamilyId
    LEFT JOIN dbo.SubFamilies sf    ON sf.SubFamilyId     = a.SubFamilyId
    WHERE ol.OrderId = @OrderId
    ORDER BY ol.OrderLineId;
END;
GO
