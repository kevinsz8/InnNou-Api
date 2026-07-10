SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- ArticlePrices has filtered unique indexes (UX_ArticlePrices_Global/UX_ArticlePrices_Contract) —
-- INSERT against a table with a filtered index requires QUOTED_IDENTIFIER ON at the session that
-- created this procedure (SQL Server compiles that setting into the proc), not just at index
-- creation time. Without this, every insert fails with error 1934.
CREATE OR ALTER PROCEDURE sp_ArticlePrice_Create
    @ArticlePriceToken UNIQUEIDENTIFIER,
    @ArticleId         INT,
    @OrganizationId    INT            = NULL,
    @Price             DECIMAL(18,4),
    @CurrencyCode      VARCHAR(3),
    @EffectiveDate     DATE,
    @Notes             NVARCHAR(500)  = NULL,
    @CreatedBy         VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ArticlePrices
        (ArticlePriceToken, ArticleId, OrganizationId, Price, CurrencyCode, EffectiveDate, Notes, CreatedBy)
    VALUES
        (@ArticlePriceToken, @ArticleId, @OrganizationId, @Price, @CurrencyCode, @EffectiveDate, @Notes, @CreatedBy);

    SELECT
        p.ArticlePriceId, p.ArticlePriceToken,
        p.ArticleId,      a.ArticleToken,
        p.OrganizationId, o.OrganizationToken,
        p.Price, p.CurrencyCode, p.EffectiveDate, p.Notes,
        p.CreatedUtc, p.CreatedBy
    FROM      ArticlePrices  p
    JOIN      Articles       a ON a.ArticleId = p.ArticleId
    LEFT JOIN Organizations  o ON o.OrganizationId = p.OrganizationId
    WHERE     p.ArticlePriceToken = @ArticlePriceToken;
END;
