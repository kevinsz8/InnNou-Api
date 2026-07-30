CREATE OR ALTER PROCEDURE sp_Family_SetDefaultTaxCategory
    @FamilyToken       uniqueidentifier,
    @DefaultTaxCategoryId int,
    @LastUpdatedBy     nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Families WHERE FamilyToken = @FamilyToken)
    BEGIN
        RAISERROR('FAMILY_NOT_FOUND', 16, 1);
        RETURN;
    END

    -- Deliberately no IsSystem guard, unlike sp_Family_Update/sp_Family_SetActive —
    -- assigning a tax category isn't a structural-identity change (Code/IsActive),
    -- so system families (BEVERAGES/FOOD/CLEANING) must remain configurable here.
    IF NOT EXISTS (SELECT 1 FROM TaxCategories WHERE TaxCategoryId = @DefaultTaxCategoryId)
    BEGIN
        RAISERROR('TAX_CATEGORY_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE Families
    SET    DefaultTaxCategoryId = @DefaultTaxCategoryId,
           LastUpdatedUtc       = SYSUTCDATETIME(),
           LastUpdatedBy        = @LastUpdatedBy
    WHERE  FamilyToken = @FamilyToken;

    SELECT
        f.FamilyId,
        f.FamilyToken,
        f.Code,
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
