SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ARTICLE PACKAGING LEVEL - GET BY ARTICLE IDS (batch)
   Used by ExportArticlesAsync to fetch every exported article's
   packaging chain in one round trip instead of one query per row
   — same STRING_SPLIT convention as sp_User_GetPaged's
   @RoleIds/@OrganizationIds (see CLAUDE.md, "Multi-value list
   filters"), applied here to avoid an N+1 pattern on export.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ArticlePackagingLevel_GetByArticleIds
(
    @ArticleIds VARCHAR(MAX)
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
    WHERE apl.ArticleId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@ArticleIds, ','))
    ORDER BY apl.ArticleId, apl.SequenceOrder;
END;
GO
