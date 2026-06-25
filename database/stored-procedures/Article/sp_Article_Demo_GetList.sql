/* =============================================================
   ARTICLE (DEMO) - GET LIST
   Returns a flat list of articles with their supplier, current
   price, generated unit presentation string, and root unit.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Article_Demo_GetList
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH UnitChain AS
    (
        SELECT
            au.ArticleUnitId, au.ArticleId, au.Level,
            au.ConversionFactor, au.IsRoot,
            uom.Name AS UnitName, uom.Abbreviation,
            CAST(1.0 AS DECIMAL(28,10)) AS CumulativeQty
        FROM dbo.ArticleUnits au
        JOIN dbo.UnitsOfMeasurement uom ON au.UnitOfMeasurementId = uom.UnitOfMeasurementId
        WHERE au.IsRoot = 1 AND au.IsActive = 1

        UNION ALL

        SELECT
            child.ArticleUnitId, child.ArticleId, child.Level,
            child.ConversionFactor, child.IsRoot,
            uom.Name, uom.Abbreviation,
            CAST(uc.CumulativeQty * child.ConversionFactor AS DECIMAL(28,10))
        FROM dbo.ArticleUnits child
        JOIN dbo.UnitsOfMeasurement uom ON child.UnitOfMeasurementId = uom.UnitOfMeasurementId
        JOIN UnitChain uc ON child.ParentArticleUnitId = uc.ArticleUnitId
        WHERE child.IsActive = 1
    ),
    GeneratedPresentation AS
    (
        SELECT
            ArticleId,
            STRING_AGG(
                CAST(
                    CASE WHEN CumulativeQty = FLOOR(CumulativeQty)
                         THEN FORMAT(CAST(CumulativeQty AS BIGINT), 'N0')
                         ELSE CONVERT(VARCHAR(30), CAST(CumulativeQty AS FLOAT))
                    END + ' ' + Abbreviation
                AS NVARCHAR(MAX)), ' = '
            ) WITHIN GROUP (ORDER BY Level) AS Presentation
        FROM UnitChain
        GROUP BY ArticleId
    ),
    CurrentPrice AS
    (
        SELECT ArticleId, Price, CurrencyCode
        FROM dbo.ArticlePriceHistory
        WHERE ValidToUtc IS NULL
    ),
    RootUnit AS
    (
        SELECT au.ArticleId, uom.Name AS RootUnitName, uom.Abbreviation AS RootUnitAbbr
        FROM dbo.ArticleUnits au
        JOIN dbo.UnitsOfMeasurement uom ON au.UnitOfMeasurementId = uom.UnitOfMeasurementId
        WHERE au.IsRoot = 1 AND au.IsActive = 1
    )
    SELECT
        s.Name              AS Supplier,
        a.ArticleId,
        a.Name              AS Article,
        a.SupplierSku,
        ru.RootUnitName,
        ru.RootUnitAbbr,
        cp.Price            AS CurrentPrice,
        cp.CurrencyCode     AS Currency,
        gp.Presentation     AS GeneratedPresentation,
        a.IsActive
    FROM dbo.Articles a
    JOIN  dbo.Suppliers            s  ON a.SupplierId  = s.SupplierId
    LEFT JOIN GeneratedPresentation gp ON a.ArticleId  = gp.ArticleId
    LEFT JOIN CurrentPrice          cp ON a.ArticleId  = cp.ArticleId
    LEFT JOIN RootUnit              ru ON a.ArticleId  = ru.ArticleId
    WHERE a.IsDeleted = 0
    ORDER BY s.Name, a.Name;
END;
GO
