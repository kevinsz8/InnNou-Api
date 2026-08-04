SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- ARTICLE PRICE - GET CHANGE NOTIFICATION INFO
-- Called right after one or more global-list ArticlePrice rows are created (single create, or
-- every successfully-created row from one BulkImportArticlePricesAsync call) to build the
-- SUPPLIER_PRICE_UPDATED notification. @ArticlePriceIds is comma-delimited so a bulk import's
-- entire batch resolves in one round-trip instead of one query per row. PreviousPrice/
-- PreviousCurrencyCode is the most recent GLOBAL price that was effective before this one (NULL
-- if this is the article's first-ever global price) — the caller only renders a %-change when
-- PreviousCurrencyCode matches NewCurrencyCode, a currency switch makes a raw percent misleading.
-- Only ever resolves OrganizationId IS NULL rows — contract (per-organization) prices never
-- trigger this notification, see the migration's own header comment for why.
-- =============================================================
CREATE OR ALTER PROCEDURE sp_ArticlePrice_GetChangeNotificationInfo
    @ArticlePriceIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Target AS
    (
        SELECT TRY_CAST(value AS INT) AS ArticlePriceId FROM STRING_SPLIT(@ArticlePriceIds, ',')
    )
    SELECT
        p.ArticlePriceId,
        p.ArticleId,      a.ArticleToken,  a.Name AS ArticleName,
        a.SupplierId,     s.SupplierToken, s.Name AS SupplierName,
        p.Price           AS NewPrice,
        p.CurrencyCode    AS NewCurrencyCode,
        p.EffectiveDate,
        prev.Price        AS PreviousPrice,
        prev.CurrencyCode AS PreviousCurrencyCode
    FROM   Target t
    JOIN   ArticlePrices p ON p.ArticlePriceId = t.ArticlePriceId
    JOIN   Articles      a ON a.ArticleId      = p.ArticleId
    JOIN   Suppliers     s ON s.SupplierId     = a.SupplierId
    OUTER APPLY (
        SELECT TOP (1) pp.Price, pp.CurrencyCode
        FROM   ArticlePrices pp
        WHERE  pp.ArticleId      = p.ArticleId
          AND  pp.OrganizationId IS NULL
          AND  pp.EffectiveDate   < p.EffectiveDate
        ORDER  BY pp.EffectiveDate DESC, pp.ArticlePriceId DESC
    ) prev
    WHERE p.OrganizationId IS NULL;
END;
GO
