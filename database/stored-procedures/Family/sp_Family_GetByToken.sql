CREATE OR ALTER PROCEDURE sp_Family_GetByToken
    @FamilyToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        f.FamilyId,
        f.FamilyToken,
        f.Code,
        f.NameTranslations,
        f.IsSystem,
        f.IsActive,
        f.DefaultTaxCategoryId,
        tc.TaxCategoryToken AS DefaultTaxCategoryToken,
        tc.Code AS DefaultTaxCategoryCode,
        f.CreatedUtc,
        f.CreatedBy,
        f.LastUpdatedUtc,
        f.LastUpdatedBy
    FROM Families f
    LEFT JOIN TaxCategories tc ON tc.TaxCategoryId = f.DefaultTaxCategoryId
    WHERE f.FamilyToken = @FamilyToken;
END;
