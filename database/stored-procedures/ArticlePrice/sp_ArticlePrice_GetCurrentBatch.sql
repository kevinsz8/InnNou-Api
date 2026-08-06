SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- ARTICLE PRICE - GET CURRENT (BATCH)
-- Batched sibling of sp_ArticlePrice_GetCurrent, for read paths that need
-- the current price of many Articles at once (e.g. the price-comparison
-- report) without looping one round trip per Article. Currency is resolved
-- ONCE for the whole call via sp_Organization_ResolveCurrencyCode -- same
-- "never blend currencies" rule the rest of the codebase already follows
-- (see Dashboard's own per-currency reporting) -- so an Article whose only
-- price is in a different currency is simply omitted, exactly like the
-- single-row version returns nothing when @CurrencyCode can't resolve.
-- Same contract-price-wins-over-global tie-break as the single-row SP,
-- via ROW_NUMBER() instead of TOP 1 per Article.
-- =============================================================
CREATE OR ALTER PROCEDURE sp_ArticlePrice_GetCurrentBatch
    @ArticleIds     VARCHAR(MAX),
    @OrganizationId INT,
    @AsOfDate       DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrencyCode VARCHAR(10);
    EXEC dbo.sp_Organization_ResolveCurrencyCode @OrganizationId, @CurrencyCode OUTPUT;

    IF @CurrencyCode IS NULL
        RETURN;

    ;WITH TargetArticles AS (
        SELECT CAST(value AS INT) AS ArticleId FROM STRING_SPLIT(@ArticleIds, ',')
    ),
    RankedPrices AS (
        SELECT p.ArticleId, p.Price, p.CurrencyCode,
               ROW_NUMBER() OVER (
                   PARTITION BY p.ArticleId
                   ORDER BY CASE WHEN p.OrganizationId IS NOT NULL THEN 0 ELSE 1 END,
                            p.EffectiveDate DESC, p.ArticlePriceId DESC
               ) AS RowNum
        FROM   ArticlePrices p
        JOIN   TargetArticles t ON t.ArticleId = p.ArticleId
        WHERE  p.CurrencyCode = @CurrencyCode
          AND  (p.OrganizationId = @OrganizationId OR p.OrganizationId IS NULL)
          AND  p.EffectiveDate <= @AsOfDate
    )
    SELECT ArticleId, Price, CurrencyCode
    FROM   RankedPrices
    WHERE  RowNum = 1;
END;
GO
