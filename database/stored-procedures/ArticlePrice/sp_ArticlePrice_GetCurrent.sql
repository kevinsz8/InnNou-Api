CREATE OR ALTER PROCEDURE sp_ArticlePrice_GetCurrent
    @ArticleId      INT,
    @OrganizationId INT           = NULL,
    @CurrencyCode   VARCHAR(10)   = NULL OUTPUT,  -- caller override; when NULL and @OrganizationId is set, resolved from the org's own or nearest ancestor's CurrencyCode (sp_Organization_ResolveCurrencyCode).
                                                   -- Still NULL on exit = no currency could be determined (no override and no org in the hierarchy has one set) — the caller must ask for a currency explicitly.
    @AsOfDate       DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @CurrencyCode IS NULL AND @OrganizationId IS NOT NULL
        EXEC dbo.sp_Organization_ResolveCurrencyCode @OrganizationId, @CurrencyCode OUTPUT;

    IF @CurrencyCode IS NULL
        RETURN;

    -- Organization-specific (contract) price takes precedence over the global price
    -- when both exist and are effective as of the requested date.
    SELECT TOP 1
        p.ArticlePriceId, p.ArticlePriceToken,
        p.ArticleId,      a.ArticleToken,
        p.OrganizationId, o.OrganizationToken,
        p.Price, p.CurrencyCode, p.EffectiveDate, p.Notes,
        p.CreatedUtc, p.CreatedBy
    FROM      ArticlePrices  p
    JOIN      Articles       a ON a.ArticleId = p.ArticleId
    LEFT JOIN Organizations  o ON o.OrganizationId = p.OrganizationId
    WHERE     p.ArticleId    = @ArticleId
      AND     p.CurrencyCode = @CurrencyCode
      AND     (p.OrganizationId = @OrganizationId OR p.OrganizationId IS NULL)
      AND     p.EffectiveDate <= @AsOfDate
    ORDER BY
        CASE WHEN p.OrganizationId IS NOT NULL THEN 0 ELSE 1 END,
        p.EffectiveDate DESC,
        p.ArticlePriceId DESC;
END;
