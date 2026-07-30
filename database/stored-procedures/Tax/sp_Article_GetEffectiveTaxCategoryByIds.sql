SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ARTICLE - GET EFFECTIVE TAX CATEGORY BY IDS (batch)
   Resolves COALESCE(Article.TaxCategoryId, Family.DefaultTaxCategoryId) for a
   list of ArticleIds in one round trip — same STRING_SPLIT batch convention
   as sp_Article_GetFamilyIdsByArticleIds, used by
   PurchaseOrderService.CreateGoodsReceiptAsync to avoid one query per line.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Article_GetEffectiveTaxCategoryByIds
(
    @ArticleIds VARCHAR(MAX)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ArticleId,
        COALESCE(a.TaxCategoryId, f.DefaultTaxCategoryId) AS TaxCategoryId,
        tc.Code AS TaxCategoryCode
    FROM dbo.Articles a
    LEFT JOIN dbo.Families f       ON f.FamilyId = a.FamilyId
    LEFT JOIN dbo.TaxCategories tc ON tc.TaxCategoryId = COALESCE(a.TaxCategoryId, f.DefaultTaxCategoryId)
    WHERE a.ArticleId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@ArticleIds, ','));
END;
GO
