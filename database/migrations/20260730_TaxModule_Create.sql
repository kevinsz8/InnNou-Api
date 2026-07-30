SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: Tax module (Phase A of TODO.md point 5, Facturacion)

   Introduces a real Category/Jurisdiction/Rate tax model so a future Goods
   Receipt (and eventually an Invoice) can compute IVA/IGIC/IGI without
   guessing. Modeled on the industry-standard "category-level default with a
   per-item override" shape (Odoo Product Category tax, Avalara tax codes):

     Families.DefaultTaxCategoryId  -- cascades to every Article in the family
     Articles.TaxCategoryId         -- optional per-article override
     Warehouses.TaxJurisdictionId   -- which jurisdiction's rates apply to a
                                        receipt at that warehouse

   TaxRates holds the CURRENT rate for a (Jurisdiction, Category) pair — not
   historized. Tax rate changes are rare and, when they happen, are an
   explicit admin edit; this is not modeled like ArticlePrices' insert-only
   history because there is no per-order "as of this date" requirement here.

   Jurisdiction rates seeded only where independently verified:
     ES_MAINLAND_BALEARIC : IVA General 21% / Reducido 10% / Superreducido 4% / Exento 0%
     ES_CANARY            : IGIC General 7% / Reducido 3% / Tipo Cero 0% / Exento 0%
     AD_STANDARD          : IGI General 4.5% / Reducido 1% / Superreducido 0% / Exento 0%

   ES_CEUTA / ES_MELILLA jurisdictions are created but deliberately left with
   NO TaxRates rows — IPSI varies by municipal ordinance and no rate is
   fabricated here. An admin must configure it via the Impuestos page before
   a Ceuta/Melilla warehouse can receive goods.

   Every existing Family is backfilled to GENERAL (the legally-correct
   residual default) and every existing Warehouse to ES_MAINLAND_BALEARIC
   (verified all current warehouses are on the Spanish mainland) so existing
   GoodsReceipt flows keep working immediately after deploy instead of
   breaking until manually reconfigured.

   GoodsReceiptLine gains a nullable tax snapshot (Category/Rate/Percent/
   Taxable/Tax/Total amounts) populated only for receipts created after this
   migration — historical lines stay null, same convention as
   DeliveryNoteNumber's own backfill migration.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TaxCategories')
BEGIN
    CREATE TABLE TaxCategories
    (
        TaxCategoryId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TaxCategoryToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        Code             VARCHAR(20)       NOT NULL,
        IsActive         BIT               NOT NULL DEFAULT (1),

        CONSTRAINT UQ_TaxCategories_Code UNIQUE (Code),
        CONSTRAINT UQ_TaxCategories_Token UNIQUE (TaxCategoryToken)
    );
END
GO

-- Seed order matters — the C# TaxCategory enum hardcodes these Ids
-- (General=1, Reduced=2, Super_Reduced=3, Exempt=4).
IF NOT EXISTS (SELECT 1 FROM TaxCategories WHERE Code = 'GENERAL')
    INSERT INTO TaxCategories (Code) VALUES ('GENERAL');
GO
IF NOT EXISTS (SELECT 1 FROM TaxCategories WHERE Code = 'REDUCED')
    INSERT INTO TaxCategories (Code) VALUES ('REDUCED');
GO
IF NOT EXISTS (SELECT 1 FROM TaxCategories WHERE Code = 'SUPER_REDUCED')
    INSERT INTO TaxCategories (Code) VALUES ('SUPER_REDUCED');
GO
IF NOT EXISTS (SELECT 1 FROM TaxCategories WHERE Code = 'EXEMPT')
    INSERT INTO TaxCategories (Code) VALUES ('EXEMPT');
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TaxJurisdictions')
BEGIN
    CREATE TABLE TaxJurisdictions
    (
        TaxJurisdictionId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TaxJurisdictionToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        Code                 VARCHAR(30)       NOT NULL,
        CountryId            INT               NOT NULL,
        Name                 VARCHAR(100)      NOT NULL,
        IsActive             BIT               NOT NULL DEFAULT (1),

        CONSTRAINT UQ_TaxJurisdictions_Code UNIQUE (Code),
        CONSTRAINT UQ_TaxJurisdictions_Token UNIQUE (TaxJurisdictionToken),
        CONSTRAINT FK_TaxJurisdictions_Countries FOREIGN KEY (CountryId) REFERENCES Countries (CountryId)
    );
END
GO

-- Seed order matters — the C# TaxJurisdiction enum hardcodes these Ids
-- (Es_Mainland_Balearic=1, Es_Canary=2, Es_Ceuta=3, Es_Melilla=4, Ad_Standard=5).
IF NOT EXISTS (SELECT 1 FROM TaxJurisdictions WHERE Code = 'ES_MAINLAND_BALEARIC')
    INSERT INTO TaxJurisdictions (Code, CountryId, Name)
    SELECT 'ES_MAINLAND_BALEARIC', CountryId, 'Espana peninsular y Baleares' FROM Countries WHERE Code = 'ES';
GO
IF NOT EXISTS (SELECT 1 FROM TaxJurisdictions WHERE Code = 'ES_CANARY')
    INSERT INTO TaxJurisdictions (Code, CountryId, Name)
    SELECT 'ES_CANARY', CountryId, 'Canarias' FROM Countries WHERE Code = 'ES';
GO
IF NOT EXISTS (SELECT 1 FROM TaxJurisdictions WHERE Code = 'ES_CEUTA')
    INSERT INTO TaxJurisdictions (Code, CountryId, Name)
    SELECT 'ES_CEUTA', CountryId, 'Ceuta' FROM Countries WHERE Code = 'ES';
GO
IF NOT EXISTS (SELECT 1 FROM TaxJurisdictions WHERE Code = 'ES_MELILLA')
    INSERT INTO TaxJurisdictions (Code, CountryId, Name)
    SELECT 'ES_MELILLA', CountryId, 'Melilla' FROM Countries WHERE Code = 'ES';
GO
IF NOT EXISTS (SELECT 1 FROM TaxJurisdictions WHERE Code = 'AD_STANDARD')
    INSERT INTO TaxJurisdictions (Code, CountryId, Name)
    SELECT 'AD_STANDARD', CountryId, 'Andorra' FROM Countries WHERE Code = 'AD';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TaxRates')
BEGIN
    CREATE TABLE TaxRates
    (
        TaxRateId        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TaxRateToken      UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        TaxJurisdictionId INT               NOT NULL,
        TaxCategoryId     INT               NOT NULL,
        RatePercent       DECIMAL(6,3)      NOT NULL,
        IsActive          BIT               NOT NULL DEFAULT (1),
        CreatedUtc        DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy         VARCHAR(150)      NOT NULL,
        LastUpdatedUtc    DATETIME2         NULL,
        LastUpdatedBy     VARCHAR(150)      NULL,

        CONSTRAINT UQ_TaxRates_Token UNIQUE (TaxRateToken),
        CONSTRAINT UQ_TaxRates_JurisdictionCategory UNIQUE (TaxJurisdictionId, TaxCategoryId),
        CONSTRAINT FK_TaxRates_TaxJurisdictions FOREIGN KEY (TaxJurisdictionId) REFERENCES TaxJurisdictions (TaxJurisdictionId),
        CONSTRAINT FK_TaxRates_TaxCategories FOREIGN KEY (TaxCategoryId) REFERENCES TaxCategories (TaxCategoryId),
        CONSTRAINT CK_TaxRates_RatePercent CHECK (RatePercent >= 0 AND RatePercent <= 100)
    );
END
GO

-- Seed the three independently-verified jurisdictions only. Ceuta/Melilla are
-- deliberately left without rates — see migration header.
IF NOT EXISTS (SELECT 1 FROM TaxRates r JOIN TaxJurisdictions j ON j.TaxJurisdictionId = r.TaxJurisdictionId WHERE j.Code = 'ES_MAINLAND_BALEARIC')
BEGIN
    INSERT INTO TaxRates (TaxJurisdictionId, TaxCategoryId, RatePercent, CreatedBy)
    SELECT j.TaxJurisdictionId, c.TaxCategoryId, v.RatePercent, 'System'
    FROM TaxJurisdictions j
    CROSS JOIN (VALUES ('GENERAL', 21.000), ('REDUCED', 10.000), ('SUPER_REDUCED', 4.000), ('EXEMPT', 0.000)) AS v(Code, RatePercent)
    JOIN TaxCategories c ON c.Code = v.Code
    WHERE j.Code = 'ES_MAINLAND_BALEARIC';
END
GO

IF NOT EXISTS (SELECT 1 FROM TaxRates r JOIN TaxJurisdictions j ON j.TaxJurisdictionId = r.TaxJurisdictionId WHERE j.Code = 'ES_CANARY')
BEGIN
    INSERT INTO TaxRates (TaxJurisdictionId, TaxCategoryId, RatePercent, CreatedBy)
    SELECT j.TaxJurisdictionId, c.TaxCategoryId, v.RatePercent, 'System'
    FROM TaxJurisdictions j
    CROSS JOIN (VALUES ('GENERAL', 7.000), ('REDUCED', 3.000), ('SUPER_REDUCED', 0.000), ('EXEMPT', 0.000)) AS v(Code, RatePercent)
    JOIN TaxCategories c ON c.Code = v.Code
    WHERE j.Code = 'ES_CANARY';
END
GO

IF NOT EXISTS (SELECT 1 FROM TaxRates r JOIN TaxJurisdictions j ON j.TaxJurisdictionId = r.TaxJurisdictionId WHERE j.Code = 'AD_STANDARD')
BEGIN
    INSERT INTO TaxRates (TaxJurisdictionId, TaxCategoryId, RatePercent, CreatedBy)
    SELECT j.TaxJurisdictionId, c.TaxCategoryId, v.RatePercent, 'System'
    FROM TaxJurisdictions j
    CROSS JOIN (VALUES ('GENERAL', 4.500), ('REDUCED', 1.000), ('SUPER_REDUCED', 0.000), ('EXEMPT', 0.000)) AS v(Code, RatePercent)
    JOIN TaxCategories c ON c.Code = v.Code
    WHERE j.Code = 'AD_STANDARD';
END
GO

-- Families: default tax category, cascades to every Article in the family unless overridden.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Families') AND name = 'DefaultTaxCategoryId')
BEGIN
    ALTER TABLE Families ADD DefaultTaxCategoryId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Families_TaxCategories')
BEGIN
    ALTER TABLE Families ADD CONSTRAINT FK_Families_TaxCategories FOREIGN KEY (DefaultTaxCategoryId) REFERENCES TaxCategories (TaxCategoryId);
END
GO

UPDATE f
SET f.DefaultTaxCategoryId = c.TaxCategoryId
FROM Families f
JOIN TaxCategories c ON c.Code = 'GENERAL'
WHERE f.DefaultTaxCategoryId IS NULL;
GO

-- Articles: optional per-article override of the family default.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'TaxCategoryId')
BEGIN
    ALTER TABLE Articles ADD TaxCategoryId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Articles_TaxCategories')
BEGIN
    ALTER TABLE Articles ADD CONSTRAINT FK_Articles_TaxCategories FOREIGN KEY (TaxCategoryId) REFERENCES TaxCategories (TaxCategoryId);
END
GO

-- Warehouses: which jurisdiction's rates apply to a receipt at that warehouse.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Warehouses') AND name = 'TaxJurisdictionId')
BEGIN
    ALTER TABLE Warehouses ADD TaxJurisdictionId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Warehouses_TaxJurisdictions')
BEGIN
    ALTER TABLE Warehouses ADD CONSTRAINT FK_Warehouses_TaxJurisdictions FOREIGN KEY (TaxJurisdictionId) REFERENCES TaxJurisdictions (TaxJurisdictionId);
END
GO

UPDATE w
SET w.TaxJurisdictionId = j.TaxJurisdictionId
FROM Warehouses w
JOIN TaxJurisdictions j ON j.Code = 'ES_MAINLAND_BALEARIC'
WHERE w.TaxJurisdictionId IS NULL;
GO

-- GoodsReceiptLine: tax snapshot, computed and frozen at receipt time. Nullable —
-- historical lines created before this migration stay null, only new receipts populate it.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('GoodsReceiptLine') AND name = 'TaxCategoryId')
BEGIN
    ALTER TABLE GoodsReceiptLine ADD
        TaxCategoryId   INT           NULL,
        TaxRateId       INT           NULL,
        TaxRatePercent  DECIMAL(6,3)  NULL,
        TaxableAmount   DECIMAL(18,4) NULL,
        TaxAmount       DECIMAL(18,4) NULL,
        TotalAmount     DECIMAL(18,4) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GoodsReceiptLine_TaxCategories')
BEGIN
    ALTER TABLE GoodsReceiptLine ADD CONSTRAINT FK_GoodsReceiptLine_TaxCategories FOREIGN KEY (TaxCategoryId) REFERENCES TaxCategories (TaxCategoryId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GoodsReceiptLine_TaxRates')
BEGIN
    ALTER TABLE GoodsReceiptLine ADD CONSTRAINT FK_GoodsReceiptLine_TaxRates FOREIGN KEY (TaxRateId) REFERENCES TaxRates (TaxRateId);
END
GO

PRINT '=== Migration 20260730_TaxModule_Create completed successfully ===';
GO
