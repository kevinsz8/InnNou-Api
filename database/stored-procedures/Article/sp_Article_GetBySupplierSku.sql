/* =============================================================
   ARTICLE - GET BY SUPPLIER SKU
   Returns a single article by (SupplierId, SupplierSku) — mirrors
   sp_Article_GetByToken's SELECT shape exactly. Used by Article and
   ArticlePrice bulk import to resolve an Excel "SupplierSku" column
   (scoped to the resolved SupplierId) to an existing article, both
   to decide insert-vs-update for Article bulk import and to target
   the correct article for a bulk-imported price row. Does not
   filter IsActive — matches sp_Article_GetByToken, which also only
   filters IsDeleted so a caller can still resolve (and, for prices,
   read the article's ReplacedByArticleId) for an inactive article.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_Article_GetBySupplierSku
    @SupplierId  INT,
    @SupplierSku VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ArticleId,
        a.ArticleToken,
        a.SupplierId,
        s.Name          AS SupplierName,
        a.Name,
        a.NormalizedName,
        a.Description,
        a.SupplierSku,
        a.Barcode,
        a.Brand,
        a.FamilyId,
        f.Code          AS FamilyCode,
        a.SubFamilyId,
        sf.Code         AS SubFamilyCode,
        a.PurchaseUnitId,
        pu.Code         AS PurchaseUnitCode,
        pu.Symbol       AS PurchaseUnitSymbol,
        a.MinimumOrderQty,
        a.LeadTimeDays,
        a.IsActive,
        a.IsDeleted,
        a.ReplacedByArticleId,
        r.ArticleToken  AS ReplacedByArticleToken
    FROM   Articles        a
    JOIN   Suppliers       s  ON  s.SupplierId       = a.SupplierId
    JOIN   UnitsOfMeasure  pu ON  pu.UnitOfMeasureId = a.PurchaseUnitId
    LEFT JOIN Families     f  ON  f.FamilyId         = a.FamilyId
    LEFT JOIN SubFamilies  sf ON  sf.SubFamilyId      = a.SubFamilyId
    LEFT JOIN Articles     r  ON  r.ArticleId         = a.ReplacedByArticleId
    WHERE  a.SupplierId  = @SupplierId
      AND  a.SupplierSku = @SupplierSku
      AND  a.IsDeleted   = 0;
END;
GO
