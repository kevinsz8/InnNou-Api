SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXCATEGORY - GET ALL
   Small fixed lookup set (4 rows) — feeds the Family/Article edit forms'
   tax-category dropdown.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxCategory_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TaxCategoryId, TaxCategoryToken, Code, IsActive
    FROM dbo.TaxCategories
    WHERE IsActive = 1
    ORDER BY TaxCategoryId;
END;
GO
