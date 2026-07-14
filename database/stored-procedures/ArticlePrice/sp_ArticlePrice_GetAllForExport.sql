/* =============================================================
   ARTICLEPRICE - GET ALL FOR EXPORT
   ArticlePrices is an insert-only historical log (never updated),
   so "export" here means dumping the rows themselves — unlike
   sp_ArticlePrice_GetHistory (scoped to one @ArticleId), this lists
   every price row visible to the caller across their whole catalog.
   @SupplierId = NULL means unrestricted (Admin+); otherwise scoped
   to that supplier's own articles only. Denormalizes SupplierName/
   ArticleName/SupplierSku/OrganizationName so the export file is
   directly human-readable without further lookups.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_ArticlePrice_GetAllForExport
(
    @PageNumber INT,
    @PageSize   INT,
    @SupplierId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.ArticlePriceId, p.ArticlePriceToken,
        p.ArticleId,      a.ArticleToken, a.Name AS ArticleName, a.SupplierSku,
        a.SupplierId,     s.Name AS SupplierName,
        p.OrganizationId, o.OrganizationToken, o.Name AS OrganizationName,
        p.Price, p.CurrencyCode, p.EffectiveDate, p.Notes,
        p.CreatedUtc, p.CreatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM      ArticlePrices  p
    JOIN      Articles       a ON a.ArticleId = p.ArticleId
    JOIN      Suppliers      s ON s.SupplierId = a.SupplierId
    LEFT JOIN Organizations  o ON o.OrganizationId = p.OrganizationId
    WHERE     (@SupplierId IS NULL OR a.SupplierId = @SupplierId)
    ORDER BY  p.CreatedUtc DESC, p.ArticlePriceId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
