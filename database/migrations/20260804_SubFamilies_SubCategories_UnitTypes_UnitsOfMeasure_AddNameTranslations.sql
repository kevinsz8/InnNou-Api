/* =============================================================
   SUBFAMILIES / SUBCATEGORIES / UNITTYPES / UNITSOFMEASURE - ADD NameTranslations
   Rollout of the multi-language catalog names pilot (see
   20260803_Families_Categories_AddNameTranslations.sql) to the remaining
   4 catalog lookup entities, following the exact same shape: `Code`
   stays the stable machine key, `NameTranslations` is a nullable/partial
   JSON object keyed by en/es/ca, resolved client-side at display time.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubFamilies') AND name = 'NameTranslations')
BEGIN
    ALTER TABLE dbo.SubFamilies ADD NameTranslations NVARCHAR(1000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SubFamilies_NameTranslations_IsJson')
BEGIN
    ALTER TABLE dbo.SubFamilies ADD CONSTRAINT CK_SubFamilies_NameTranslations_IsJson
        CHECK (NameTranslations IS NULL OR ISJSON(NameTranslations) = 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SubCategories') AND name = 'NameTranslations')
BEGIN
    ALTER TABLE dbo.SubCategories ADD NameTranslations NVARCHAR(1000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SubCategories_NameTranslations_IsJson')
BEGIN
    ALTER TABLE dbo.SubCategories ADD CONSTRAINT CK_SubCategories_NameTranslations_IsJson
        CHECK (NameTranslations IS NULL OR ISJSON(NameTranslations) = 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UnitTypes') AND name = 'NameTranslations')
BEGIN
    ALTER TABLE dbo.UnitTypes ADD NameTranslations NVARCHAR(1000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_UnitTypes_NameTranslations_IsJson')
BEGIN
    ALTER TABLE dbo.UnitTypes ADD CONSTRAINT CK_UnitTypes_NameTranslations_IsJson
        CHECK (NameTranslations IS NULL OR ISJSON(NameTranslations) = 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UnitsOfMeasure') AND name = 'NameTranslations')
BEGIN
    ALTER TABLE dbo.UnitsOfMeasure ADD NameTranslations NVARCHAR(1000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_UnitsOfMeasure_NameTranslations_IsJson')
BEGIN
    ALTER TABLE dbo.UnitsOfMeasure ADD CONSTRAINT CK_UnitsOfMeasure_NameTranslations_IsJson
        CHECK (NameTranslations IS NULL OR ISJSON(NameTranslations) = 1);
END
GO
