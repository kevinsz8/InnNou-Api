SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ARTICLE - GET EFFECTIVE TAX CATEGORY BY IDS (batch)
   Resolves, for a list of ArticleIds in one round trip (same STRING_SPLIT
   batch convention as sp_Article_GetFamilyIdsByArticleIds, used by
   PurchaseOrderService.CreateGoodsReceiptAsync to avoid one query per line):

     COALESCE(Article.TaxCategoryId,
              FamilyTaxCategoryOverrides for (FamilyId, @TaxJurisdictionId),
              Family.DefaultTaxCategoryId)

   The middle term is new (20260805_FamilyTaxCategoryOverrides_Create.sql) —
   lets a Family (e.g. Bebidas) resolve to a different category in one
   jurisdiction than another, not just a different rate for the same
   category. Article.TaxCategoryId still wins over everything and is NOT
   jurisdiction-scoped — no driver yet for a per-article-per-country
   override.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Article_GetEffectiveTaxCategoryByIds
(
    @ArticleIds        VARCHAR(MAX),
    @TaxJurisdictionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ArticleId,
        COALESCE(a.TaxCategoryId, fo.TaxCategoryId, f.DefaultTaxCategoryId) AS TaxCategoryId,
        tc.Code AS TaxCategoryCode
    FROM dbo.Articles a
    LEFT JOIN dbo.Families f                   ON f.FamilyId = a.FamilyId
    LEFT JOIN dbo.FamilyTaxCategoryOverrides fo ON fo.FamilyId = f.FamilyId
                                                AND fo.TaxJurisdictionId = @TaxJurisdictionId
                                                AND fo.IsActive = 1
    LEFT JOIN dbo.TaxCategories tc              ON tc.TaxCategoryId = COALESCE(a.TaxCategoryId, fo.TaxCategoryId, f.DefaultTaxCategoryId)
    WHERE a.ArticleId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@ArticleIds, ','));
END;
GO
