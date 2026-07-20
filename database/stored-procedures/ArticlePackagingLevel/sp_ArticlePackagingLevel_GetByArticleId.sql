SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ARTICLE PACKAGING LEVEL - GET BY ARTICLE ID
   Ordered by SequenceOrder (1 = closest to the purchase unit). The
   last row is always the Unidad Definida (IsDefinedUnit = 1) —
   enforced by UX_ArticlePackagingLevels_Article_DefinedUnit plus
   application-layer validation, not re-checked here.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ArticlePackagingLevel_GetByArticleId
(
    @ArticleId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        apl.ArticlePackagingLevelId,
        apl.ArticlePackagingLevelToken,
        apl.ArticleId,
        apl.SequenceOrder,
        apl.UnitOfMeasureId,
        uom.Code   AS UnitOfMeasureCode,
        uom.Symbol AS UnitOfMeasureSymbol,
        apl.QuantityInParentUnit,
        apl.IsDefinedUnit,
        apl.CreatedUtc,
        apl.CreatedBy
    FROM dbo.ArticlePackagingLevels apl
    JOIN dbo.UnitsOfMeasure uom ON uom.UnitOfMeasureId = apl.UnitOfMeasureId
    WHERE apl.ArticleId = @ArticleId
    ORDER BY apl.SequenceOrder;
END;
GO
