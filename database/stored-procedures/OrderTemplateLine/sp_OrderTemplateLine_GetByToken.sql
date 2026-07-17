SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATELINE - GET BY TOKEN
   Used by OrderTemplateService.EditLineAsync/DeleteLineAsync to resolve
   a single line before re-fetching its parent OrderTemplate for
   authorization — mirrors sp_OrderLine_GetByToken.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplateLine_GetByToken
(
    @OrderTemplateLineToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        otl.OrderTemplateLineId, otl.OrderTemplateLineToken, otl.OrderTemplateId, ot.OrderTemplateToken,
        otl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        a.SupplierId, s.Name AS SupplierName, a.SupplierSku, s.SupplierType,
        a.PurchaseUnitId, pu.Code AS PurchaseUnitCode, pu.Symbol AS PurchaseUnitSymbol,
        a.IsActive AS IsArticleActive, a.IsDeleted AS IsArticleDeleted,
        r.ArticleToken AS ReplacedByArticleToken,
        otl.Quantity,
        otl.CreatedUtc, otl.CreatedBy, otl.LastUpdatedUtc, otl.LastUpdatedBy
    FROM dbo.OrderTemplateLine otl
    JOIN dbo.OrderTemplate ot   ON ot.OrderTemplateId  = otl.OrderTemplateId
    JOIN dbo.Articles a         ON a.ArticleId         = otl.ArticleId
    JOIN dbo.Suppliers s        ON s.SupplierId        = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu  ON pu.UnitOfMeasureId  = a.PurchaseUnitId
    LEFT JOIN dbo.Articles r    ON r.ArticleId         = a.ReplacedByArticleId
    WHERE otl.OrderTemplateLineToken = @OrderTemplateLineToken;
END;
GO
