/* =============================================================
   FAMILIES / CATEGORIES - ADD NameTranslations (pilot)
   Multi-language catalog names, piloted on Families/Categories before
   rolling out to SubFamilies/SubCategories/UnitTypes/UnitsOfMeasure.
   `Code` stays exactly as-is (the stable machine key — used for bulk
   import matching/uniqueness, never translated). `NameTranslations` is a
   small JSON object keyed by the app's own i18next language codes
   (`en`/`es`/`ca` — see InnNou-Web/src/i18n/index.ts), e.g.
   '{"es":"Bebidas","en":"Beverages","ca":"Begudes"}' — nullable/partial:
   any subset of the 3 keys may be present, resolution falls back to
   another present language and finally to Code at display time
   (frontend-side, matching how every other Code/Status field is already
   resolved to a display string client-side, never baked into the API
   response). Researched before building: Odoo moved from a separate
   ir_translation join-table to a JSON column per translatable field in
   v16+ specifically for this exact row-count/lookup-table shape — no
   new table, no JOIN needed in any read path.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Families') AND name = 'NameTranslations')
BEGIN
    ALTER TABLE dbo.Families ADD NameTranslations NVARCHAR(1000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Families_NameTranslations_IsJson')
BEGIN
    ALTER TABLE dbo.Families ADD CONSTRAINT CK_Families_NameTranslations_IsJson
        CHECK (NameTranslations IS NULL OR ISJSON(NameTranslations) = 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Categories') AND name = 'NameTranslations')
BEGIN
    ALTER TABLE dbo.Categories ADD NameTranslations NVARCHAR(1000) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Categories_NameTranslations_IsJson')
BEGIN
    ALTER TABLE dbo.Categories ADD CONSTRAINT CK_Categories_NameTranslations_IsJson
        CHECK (NameTranslations IS NULL OR ISJSON(NameTranslations) = 1);
END
GO
