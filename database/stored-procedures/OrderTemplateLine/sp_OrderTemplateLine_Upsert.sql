SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERTEMPLATELINE - UPSERT
   Adding an Article already present on this Template bumps its Quantity
   instead of creating a duplicate line — same "add to cart" behavior as
   sp_OrderLine_Upsert. No price/structural snapshot is ever stored —
   those are resolved fresh at apply-time via OrderService.AddLineAsync.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderTemplateLine_Upsert
(
    @OrderTemplateLineToken UNIQUEIDENTIFIER,
    @OrderTemplateId        INT,
    @ArticleId              INT,
    @Quantity               DECIMAL(18,4),
    @CreatedBy              VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.OrderTemplateLine WHERE OrderTemplateId = @OrderTemplateId AND ArticleId = @ArticleId)
    BEGIN
        UPDATE dbo.OrderTemplateLine
        SET
            Quantity       = @Quantity,
            LastUpdatedUtc = SYSUTCDATETIME(),
            LastUpdatedBy  = @CreatedBy
        WHERE OrderTemplateId = @OrderTemplateId AND ArticleId = @ArticleId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.OrderTemplateLine (OrderTemplateLineToken, OrderTemplateId, ArticleId, Quantity, CreatedBy)
        VALUES (@OrderTemplateLineToken, @OrderTemplateId, @ArticleId, @Quantity, @CreatedBy);
    END

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
    WHERE otl.OrderTemplateId = @OrderTemplateId AND otl.ArticleId = @ArticleId;
END;
GO
