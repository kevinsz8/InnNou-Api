-- =============================================================
-- MIGRATION: Create Article Discounts module (DiscountTypes,
--            ArticleDiscounts), plus the frozen-discount snapshot
--            columns on OrderLine/PurchaseOrderLine
-- Date: 2026-08-07
-- =============================================================
-- User's own idea, researched before building (SAP Condition Technique — price
-- and discount are separate master data with independent lifecycles; every
-- modern promotion engine supports SKU/category/brand-wide targeting resolved
-- "most specific wins, no stacking"). ArticlePrices is left completely
-- untouched — a Supplier configures a discount without ever re-importing a
-- price, and it auto-expires on its own EffectiveUntil with no manual revert.
--
-- Scope is exactly one of ArticleId/SubFamilyId/FamilyId, or all three NULL
-- meaning "every Article from this Supplier" — resolution priority at
-- OrderService.AddLineAsync is Article > SubFamily > Family > supplier-wide,
-- the same "most specific wins" shape already used by
-- FamilyTaxCategoryOverrides / EffectiveArticleClassification / ParLevels
-- (EVENT > SEASONAL > BASE).
--
-- Editable + toggleable (IsActive), NOT insert-only like ArticlePrices — a
-- discount is a promotion's current configuration, not a historical price
-- fact log; confirmed explicitly with the user (same shape as ParLevels/
-- FamilyApprovalThresholds).
--
-- Idempotent — safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ── DiscountTypes (Id-backed lookup, same shape as SupplierTypes/ParLevelOverrideTypes) ──
IF OBJECT_ID('DiscountTypes', 'U') IS NULL
BEGIN
    CREATE TABLE DiscountTypes (
        DiscountTypeId int         NOT NULL IDENTITY(1,1),
        Code           varchar(20) NOT NULL,
        IsActive       bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_DiscountTypes PRIMARY KEY (DiscountTypeId),
        CONSTRAINT UQ_DiscountTypes_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# DiscountType enum hardcodes these Ids (Percentage=1, FixedAmount=2),
-- and ArticleDiscounts' own CHECK constraints below hardcode the same literals.
IF NOT EXISTS (SELECT 1 FROM DiscountTypes WHERE Code = 'PERCENTAGE')
    INSERT INTO DiscountTypes (Code) VALUES ('PERCENTAGE');
GO
IF NOT EXISTS (SELECT 1 FROM DiscountTypes WHERE Code = 'FIXED_AMOUNT')
    INSERT INTO DiscountTypes (Code) VALUES ('FIXED_AMOUNT');
GO

-- ── ArticleDiscounts ────────────────────────────────────────────────────────
IF OBJECT_ID('ArticleDiscounts', 'U') IS NULL
BEGIN
    CREATE TABLE ArticleDiscounts (
        ArticleDiscountId    int              NOT NULL IDENTITY(1,1),
        ArticleDiscountToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        SupplierId           int              NOT NULL,

        -- Scope: at most one of these three set; all NULL means "every Article
        -- this Supplier sells" (see CK_ArticleDiscounts_ScopeExclusive below).
        ArticleId            int              NULL,
        SubFamilyId          int              NULL,
        FamilyId             int              NULL,

        DiscountTypeId       int              NOT NULL,
        DiscountValue        decimal(18,8)    NOT NULL,
        CurrencyCode         varchar(3)       NULL,   -- required for FIXED_AMOUNT only

        EffectiveFrom        date             NOT NULL,
        EffectiveUntil       date             NULL,   -- NULL = open-ended, until manually deactivated
        Description          nvarchar(300)    NULL,
        IsActive             bit              NOT NULL DEFAULT 1,

        CreatedUtc           datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy            varchar(150)     NOT NULL,
        LastUpdatedUtc        datetime2           NULL,
        LastUpdatedBy         varchar(150)        NULL,

        CONSTRAINT PK_ArticleDiscounts PRIMARY KEY (ArticleDiscountId),
        CONSTRAINT FK_ArticleDiscounts_Suppliers   FOREIGN KEY (SupplierId)   REFERENCES Suppliers (SupplierId),
        CONSTRAINT FK_ArticleDiscounts_Articles    FOREIGN KEY (ArticleId)    REFERENCES Articles (ArticleId),
        CONSTRAINT FK_ArticleDiscounts_SubFamilies FOREIGN KEY (SubFamilyId) REFERENCES SubFamilies (SubFamilyId),
        CONSTRAINT FK_ArticleDiscounts_Families    FOREIGN KEY (FamilyId)    REFERENCES Families (FamilyId),
        CONSTRAINT FK_ArticleDiscounts_DiscountTypes FOREIGN KEY (DiscountTypeId) REFERENCES DiscountTypes (DiscountTypeId),

        CONSTRAINT CK_ArticleDiscounts_ScopeExclusive CHECK (
            (CASE WHEN ArticleId IS NOT NULL THEN 1 ELSE 0 END +
             CASE WHEN SubFamilyId IS NOT NULL THEN 1 ELSE 0 END +
             CASE WHEN FamilyId IS NOT NULL THEN 1 ELSE 0 END) <= 1
        ),
        CONSTRAINT CK_ArticleDiscounts_DateRange CHECK (EffectiveUntil IS NULL OR EffectiveUntil >= EffectiveFrom),
        CONSTRAINT CK_ArticleDiscounts_ValuePositive CHECK (DiscountValue > 0),
        -- Type=1 (PERCENTAGE) / Type=2 (FIXED_AMOUNT) hardcoded literals, same precedent as
        -- ParLevelOverrides' own CK_ParLevelOverrides_TypeColumns.
        CONSTRAINT CK_ArticleDiscounts_PercentageMax CHECK (DiscountTypeId <> 1 OR DiscountValue <= 100),
        CONSTRAINT CK_ArticleDiscounts_CurrencyMatchesType CHECK (
            (DiscountTypeId = 1 AND CurrencyCode IS NULL) OR
            (DiscountTypeId = 2 AND CurrencyCode IS NOT NULL)
        )
    );

    CREATE UNIQUE INDEX UQ_ArticleDiscounts_ArticleDiscountToken ON ArticleDiscounts (ArticleDiscountToken);

    -- FK lookups/joins — SQL Server does not auto-index foreign keys.
    CREATE INDEX IX_ArticleDiscounts_SupplierId ON ArticleDiscounts (SupplierId);
    -- Back the resolution OUTER APPLY (one per scope dimension) and the C#-side overlap check —
    -- each filters on the scope column + IsActive first.
    CREATE INDEX IX_ArticleDiscounts_ArticleId    ON ArticleDiscounts (ArticleId, IsActive)    WHERE ArticleId IS NOT NULL;
    CREATE INDEX IX_ArticleDiscounts_SubFamilyId  ON ArticleDiscounts (SubFamilyId, IsActive)  WHERE SubFamilyId IS NOT NULL;
    CREATE INDEX IX_ArticleDiscounts_FamilyId     ON ArticleDiscounts (FamilyId, IsActive)     WHERE FamilyId IS NOT NULL;
    CREATE INDEX IX_ArticleDiscounts_SupplierWide ON ArticleDiscounts (SupplierId, IsActive)
        WHERE ArticleId IS NULL AND SubFamilyId IS NULL AND FamilyId IS NULL;
END
GO

-- ── Frozen discount snapshot on OrderLine/PurchaseOrderLine ────────────────
-- Same "freeze descriptive fields at line-add time" shape as CategoryId/CategoryCode above —
-- BaseUnitPrice NULL means no discount applied (UnitPrice IS the base price); when set, UnitPrice
-- is already the discounted price and BaseUnitPrice/DiscountTypeId/DiscountValue are the frozen
-- "why" for transparency on a historical line.
IF COL_LENGTH('dbo.OrderLine', 'BaseUnitPrice') IS NULL
    ALTER TABLE OrderLine ADD BaseUnitPrice decimal(18,8) NULL;
GO
IF COL_LENGTH('dbo.OrderLine', 'DiscountTypeId') IS NULL
    ALTER TABLE OrderLine ADD DiscountTypeId int NULL;
GO
IF COL_LENGTH('dbo.OrderLine', 'DiscountValue') IS NULL
    ALTER TABLE OrderLine ADD DiscountValue decimal(18,8) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderLine_DiscountTypes')
    ALTER TABLE OrderLine ADD CONSTRAINT FK_OrderLine_DiscountTypes FOREIGN KEY (DiscountTypeId) REFERENCES DiscountTypes (DiscountTypeId);
GO

IF COL_LENGTH('dbo.PurchaseOrderLine', 'BaseUnitPrice') IS NULL
    ALTER TABLE PurchaseOrderLine ADD BaseUnitPrice decimal(18,8) NULL;
GO
IF COL_LENGTH('dbo.PurchaseOrderLine', 'DiscountTypeId') IS NULL
    ALTER TABLE PurchaseOrderLine ADD DiscountTypeId int NULL;
GO
IF COL_LENGTH('dbo.PurchaseOrderLine', 'DiscountValue') IS NULL
    ALTER TABLE PurchaseOrderLine ADD DiscountValue decimal(18,8) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PurchaseOrderLine_DiscountTypes')
    ALTER TABLE PurchaseOrderLine ADD CONSTRAINT FK_PurchaseOrderLine_DiscountTypes FOREIGN KEY (DiscountTypeId) REFERENCES DiscountTypes (DiscountTypeId);
GO

-- ── Widen dbo.PurchaseOrderLineTableType with the same 3 columns ───────────
-- SQL Server has no ALTER TYPE — a TVP type must be dropped and recreated, which first requires
-- dropping whatever stored procedure references it in a parameter (sp_PurchaseOrderLine_CreateBatch,
-- re-applied with the new shape from its own .sql file right after this migration runs).
IF TYPE_ID(N'dbo.PurchaseOrderLineTableType') IS NOT NULL AND
   NOT EXISTS (
       SELECT 1 FROM sys.table_types tt
       JOIN sys.columns c ON c.object_id = tt.type_table_object_id
       WHERE tt.name = 'PurchaseOrderLineTableType' AND c.name = 'DiscountValue'
   )
BEGIN
    IF OBJECT_ID('dbo.sp_PurchaseOrderLine_CreateBatch', 'P') IS NOT NULL
        DROP PROCEDURE dbo.sp_PurchaseOrderLine_CreateBatch;

    DROP TYPE dbo.PurchaseOrderLineTableType;
END
GO

IF TYPE_ID(N'dbo.PurchaseOrderLineTableType') IS NULL
BEGIN
    CREATE TYPE dbo.PurchaseOrderLineTableType AS TABLE
    (
        PurchaseOrderLineToken UNIQUEIDENTIFIER NOT NULL,
        PurchaseOrderId        INT               NOT NULL,
        OrderLineId            INT               NULL,
        ArticleId              INT               NOT NULL,
        Quantity                DECIMAL(18,8)    NOT NULL,
        PurchaseUnitId          INT              NOT NULL,
        PurchaseQuantity        DECIMAL(18,8)    NOT NULL,
        ContentUnitId           INT              NOT NULL,
        ContentQuantity         DECIMAL(18,8)    NULL,
        UnitPrice               DECIMAL(18,8)    NOT NULL,
        CurrencyCode            VARCHAR(3)       NOT NULL,
        CategoryId              INT              NULL,
        CategoryCode            NVARCHAR(50)     NULL,
        SubCategoryId           INT              NULL,
        SubCategoryCode         NVARCHAR(50)     NULL,
        BaseUnitPrice           DECIMAL(18,8)    NULL,
        DiscountTypeId          INT              NULL,
        DiscountValue           DECIMAL(18,8)    NULL,
        Notes                   NVARCHAR(500)    NULL
    );
END;
GO

PRINT '=== Migration 20260807_ArticleDiscounts_Create completed successfully ===';
GO
