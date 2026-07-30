SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXCATEGORY - GET BY TOKEN
   Resolves a caller-supplied token to its internal Id — used by
   FamilyService/ArticleService when writing DefaultTaxCategoryId/TaxCategoryId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxCategory_GetByToken
(
    @TaxCategoryToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TaxCategoryId, TaxCategoryToken, Code, IsActive
    FROM dbo.TaxCategories
    WHERE TaxCategoryToken = @TaxCategoryToken;
END;
GO
