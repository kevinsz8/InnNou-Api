CREATE OR ALTER PROCEDURE sp_ArticlePrice_GetHistory
(
    @PageNumber                     INT,
    @PageSize                       INT,
    @ArticleId                      INT,
    @OrganizationId                 INT        = NULL,  -- optional filter; forced to the caller's own org when @UnrestrictedOrganizationAccess = 0
    @CurrencyCode                   VARCHAR(3) = NULL,
    @UnrestrictedOrganizationAccess BIT        = 0       -- 1 for the owning supplier/Admin (sees every organization's contract prices)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.ArticlePriceId, p.ArticlePriceToken,
        p.ArticleId,      a.ArticleToken,
        p.OrganizationId, o.OrganizationToken,
        p.Price, p.CurrencyCode, p.EffectiveDate, p.Notes,
        p.CreatedUtc, p.CreatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM      ArticlePrices  p
    JOIN      Articles       a ON a.ArticleId = p.ArticleId
    LEFT JOIN Organizations  o ON o.OrganizationId = p.OrganizationId
    WHERE     p.ArticleId = @ArticleId
      AND     (@CurrencyCode IS NULL OR p.CurrencyCode = @CurrencyCode)
      AND     (
                p.OrganizationId IS NULL                                                                       -- global rows always visible
                OR (@UnrestrictedOrganizationAccess = 1 AND (@OrganizationId IS NULL OR p.OrganizationId = @OrganizationId))
                OR (@UnrestrictedOrganizationAccess = 0 AND p.OrganizationId = @OrganizationId)
              )
    ORDER BY p.EffectiveDate DESC, p.ArticlePriceId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
