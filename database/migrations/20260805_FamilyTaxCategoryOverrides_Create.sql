SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: Family tax category overrides, per jurisdiction

   Closes a real gap in the Tax module (see .claude/GoodsReceiptsModule.md's
   "Tax" section): Families.DefaultTaxCategoryId is a single GLOBAL column,
   so a Family (e.g. Bebidas) could not resolve to a different tax category
   in one country than in another (e.g. reduced-rate in Spain, general-rate
   in a future Costa Rica/El Salvador jurisdiction) — only the RATE varied by
   jurisdiction, never the CATEGORY itself.

   Researched before building: SAP (per-country material tax classification
   on the Sales Org tab), Odoo (Fiscal Positions remapping a product's
   default tax per destination country), and Avalara (per-jurisdiction
   taxability matrix for the same universal tax code) all decouple
   "classification" from "country" but let the country dimension override
   which bucket a product lands in — none rely on a single global category
   column. FamilyTaxCategoryOverrides is InnNou's equivalent of that
   override table.

   Resolution precedence (most specific wins), extending the existing
   COALESCE(Article.TaxCategoryId, Family.DefaultTaxCategoryId) chain:

     COALESCE(Article.TaxCategoryId,
              FamilyTaxCategoryOverrides for (FamilyId, the receiving
                Warehouse's TaxJurisdictionId),
              Family.DefaultTaxCategoryId)

   Article.TaxCategoryId remains a single global override (not jurisdiction-
   scoped) — no real driver for a per-article-per-country override exists
   yet; only Family-level needed one, per Costa Rica/El Salvador research.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FamilyTaxCategoryOverrides')
BEGIN
    CREATE TABLE FamilyTaxCategoryOverrides
    (
        FamilyTaxCategoryOverrideId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FamilyTaxCategoryOverrideToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        FamilyId                       INT               NOT NULL,
        TaxJurisdictionId              INT               NOT NULL,
        TaxCategoryId                  INT               NOT NULL,
        IsActive                       BIT               NOT NULL DEFAULT (1),
        CreatedUtc                     DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                      VARCHAR(150)      NOT NULL,
        LastUpdatedUtc                 DATETIME2         NULL,
        LastUpdatedBy                  VARCHAR(150)      NULL,

        CONSTRAINT UQ_FamilyTaxCategoryOverrides_Token UNIQUE (FamilyTaxCategoryOverrideToken),
        CONSTRAINT UQ_FamilyTaxCategoryOverrides_FamilyJurisdiction UNIQUE (FamilyId, TaxJurisdictionId),
        CONSTRAINT FK_FamilyTaxCategoryOverrides_Families FOREIGN KEY (FamilyId) REFERENCES Families (FamilyId),
        CONSTRAINT FK_FamilyTaxCategoryOverrides_TaxJurisdictions FOREIGN KEY (TaxJurisdictionId) REFERENCES TaxJurisdictions (TaxJurisdictionId),
        CONSTRAINT FK_FamilyTaxCategoryOverrides_TaxCategories FOREIGN KEY (TaxCategoryId) REFERENCES TaxCategories (TaxCategoryId)
    );
END
GO

PRINT '=== Migration 20260805_FamilyTaxCategoryOverrides_Create completed successfully ===';
GO
