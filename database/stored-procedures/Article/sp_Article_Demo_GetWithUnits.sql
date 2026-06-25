/* =============================================================
   ARTICLE (DEMO) - GET WITH UNITS
   Returns articles expanded by all unit chain levels, with
   per-unit pricing derived from the root unit price and
   cumulative conversion factors.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Article_Demo_GetWithUnits
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH UnitChain AS
    (
        SELECT
            au.ArticleUnitId, au.ArticleId, au.ParentArticleUnitId,
            uom.Name AS UnitName, uom.Abbreviation,
            au.Level, au.ConversionFactor, au.IsRoot,
            CAST(1.0 AS DECIMAL(28,10)) AS CumulativeDivisor,
            CAST(1.0 AS DECIMAL(28,10)) AS CumulativeQty
        FROM dbo.ArticleUnits au
        JOIN dbo.UnitsOfMeasurement uom ON au.UnitOfMeasurementId = uom.UnitOfMeasurementId
        WHERE au.IsRoot = 1 AND au.IsActive = 1

        UNION ALL

        SELECT
            child.ArticleUnitId, child.ArticleId, child.ParentArticleUnitId,
            uom.Name, uom.Abbreviation,
            child.Level, child.ConversionFactor, child.IsRoot,
            CAST(uc.CumulativeDivisor * child.ConversionFactor AS DECIMAL(28,10)),
            CAST(uc.CumulativeQty     * child.ConversionFactor AS DECIMAL(28,10))
        FROM dbo.ArticleUnits child
        JOIN dbo.UnitsOfMeasurement uom ON child.UnitOfMeasurementId = uom.UnitOfMeasurementId
        JOIN UnitChain uc ON child.ParentArticleUnitId = uc.ArticleUnitId
        WHERE child.IsActive = 1
    ),
    CurrentPrice AS
    (
        SELECT ArticleId, Price, CurrencyCode
        FROM dbo.ArticlePriceHistory
        WHERE ValidToUtc IS NULL
    )
    SELECT
        s.Name                                                          AS Supplier,
        a.ArticleId,
        a.Name                                                          AS Article,
        uc.Level,
        uc.UnitName,
        uc.Abbreviation                                                 AS UnitAbbr,
        uc.IsRoot,
        uc.ConversionFactor                                             AS CF,
        CASE
            WHEN uc.CumulativeQty = FLOOR(uc.CumulativeQty)
            THEN FORMAT(CAST(uc.CumulativeQty AS BIGINT), 'N0')
            ELSE CONVERT(VARCHAR(30), CAST(uc.CumulativeQty AS FLOAT))
        END                                                             AS QtyPerRootUnit,
        cp.Price                                                        AS RootUnitPrice,
        cp.CurrencyCode                                                 AS Currency,
        CAST(cp.Price / uc.CumulativeDivisor AS DECIMAL(18,4))         AS PricePerUnit
    FROM UnitChain uc
    JOIN dbo.Articles  a  ON uc.ArticleId  = a.ArticleId
    JOIN dbo.Suppliers s  ON a.SupplierId  = s.SupplierId
    LEFT JOIN CurrentPrice cp ON uc.ArticleId = cp.ArticleId
    WHERE a.IsDeleted = 0
    ORDER BY s.Name, a.ArticleId, uc.Level;
END;
GO
