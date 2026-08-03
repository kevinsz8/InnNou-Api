SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXCATEGORY - GET ALL
   Feeds the Family/Article edit forms' tax-category dropdown. No longer a
   fixed 4-row set — sp_TaxCategory_Create lets a SuperAdmin add more
   (e.g. Costa Rica's extra 4%/2%/1% tiers beyond GENERAL/REDUCED).
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
