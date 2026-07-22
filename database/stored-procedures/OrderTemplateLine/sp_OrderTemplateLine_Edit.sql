SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATELINE - EDIT
   Quantity-only change — mirrors sp_OrderLine_Edit.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplateLine_Edit
(
    @OrderTemplateLineToken UNIQUEIDENTIFIER,
    @Quantity               DECIMAL(18,4),
    @LastUpdatedBy          VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.OrderTemplateLine WHERE OrderTemplateLineToken = @OrderTemplateLineToken)
    BEGIN
        RAISERROR('ORDER_TEMPLATE_LINE_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE dbo.OrderTemplateLine
    SET
        Quantity       = @Quantity,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy  = @LastUpdatedBy
    WHERE OrderTemplateLineToken = @OrderTemplateLineToken;

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
    WHERE otl.OrderTemplateLineToken = @OrderTemplateLineToken;
END;
GO
