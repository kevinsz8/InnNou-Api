SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXCATEGORY - CREATE
   Lets a SuperAdmin add a tax category beyond the originally-seeded
   GENERAL/REDUCED/SUPER_REDUCED/EXEMPT four (e.g. Costa Rica's IVA has 5
   tiers — 13/4/2/1/0% — one more than Spain/Andorra's set), from the
   Impuestos admin page instead of requiring a migration. Mirrors
   sp_TaxJurisdiction_Create's shape. Created with zero TaxRates rows —
   sp_TaxRate_Upsert fills them in per jurisdiction afterward.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxCategory_Create
(
    @Code VARCHAR(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.TaxCategories WHERE Code = @Code)
    BEGIN
        RAISERROR('TAX_CATEGORY_CODE_ALREADY_EXISTS', 16, 1);
        RETURN;
    END

    DECLARE @NewId INT;

    INSERT INTO dbo.TaxCategories (TaxCategoryToken, Code, IsActive)
    VALUES (NEWID(), @Code, 1);

    SET @NewId = SCOPE_IDENTITY();

    SELECT TaxCategoryId, TaxCategoryToken, Code, IsActive
    FROM dbo.TaxCategories
    WHERE TaxCategoryId = @NewId;
END;
GO
