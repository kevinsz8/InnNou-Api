SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATELINE - GET BY ORDER TEMPLATE ID
   Denormalizes Article/Supplier/Unit fields so the caller has a directly
   readable list — mirrors sp_OrderLine_GetByOrderId. Deliberately never
   filters on Articles.IsActive/IsDeleted (unlike most reads elsewhere in
   this codebase) — a template line whose Article was since deactivated
   or soft-deleted must still surface so the edit page can badge it
   "no longer available" instead of the line silently vanishing.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplateLine_GetByOrderTemplateId
(
    @OrderTemplateId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        otl.OrderTemplateLineId, otl.OrderTemplateLineToken, otl.OrderTemplateId, ot.OrderTemplateToken,
        otl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        a.SupplierId, s.Name AS SupplierName, a.SupplierSku, st.Code AS SupplierType,
        a.PurchaseUnitId, pu.Code AS PurchaseUnitCode, pu.Symbol AS PurchaseUnitSymbol,
        a.IsActive AS IsArticleActive, a.IsDeleted AS IsArticleDeleted,
        r.ArticleToken AS ReplacedByArticleToken,
        otl.Quantity,
        otl.CreatedUtc, otl.CreatedBy, otl.LastUpdatedUtc, otl.LastUpdatedBy
    FROM dbo.OrderTemplateLine otl
    JOIN dbo.OrderTemplate ot   ON ot.OrderTemplateId  = otl.OrderTemplateId
    JOIN dbo.Articles a         ON a.ArticleId         = otl.ArticleId
    JOIN dbo.Suppliers s        ON s.SupplierId        = a.SupplierId
    JOIN dbo.SupplierTypes st   ON st.SupplierTypeId   = s.SupplierTypeId
    JOIN dbo.UnitsOfMeasure pu  ON pu.UnitOfMeasureId  = a.PurchaseUnitId
    LEFT JOIN dbo.Articles r    ON r.ArticleId         = a.ReplacedByArticleId
    WHERE otl.OrderTemplateId = @OrderTemplateId
    ORDER BY otl.OrderTemplateLineId;
END;
GO
