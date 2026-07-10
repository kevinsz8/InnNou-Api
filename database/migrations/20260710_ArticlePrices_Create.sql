-- =============================================================
-- MIGRATION: Create ArticlePrices table
-- Date: 2026-07-10
-- =============================================================
-- Insert-only historical price log for Articles. Suppliers (or an
-- Admin impersonating one) record a new row every time a price
-- changes instead of overwriting the previous one, so cost-over-time
-- reports can reconstruct "what did this cost on date X" later.
-- OrganizationId is nullable: NULL = global list price, SET = an
-- organization-specific contract price that takes precedence over
-- the global price when resolving "current price".
-- Guarded so it is a no-op if already applied.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('ArticlePrices', 'U') IS NULL
BEGIN
    CREATE TABLE ArticlePrices
    (
        ArticlePriceId    INT              IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ArticlePriceToken UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID(),
        ArticleId         INT              NOT NULL,
        OrganizationId    INT              NULL,
        Price             DECIMAL(18,4)    NOT NULL,
        CurrencyCode      VARCHAR(3)       NOT NULL,
        EffectiveDate     DATE             NOT NULL,
        Notes             NVARCHAR(500)    NULL,
        CreatedUtc        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy         VARCHAR(150)     NOT NULL,

        CONSTRAINT FK_ArticlePrices_Article      FOREIGN KEY (ArticleId)      REFERENCES Articles (ArticleId),
        CONSTRAINT FK_ArticlePrices_Organization FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
    );
END
GO

-- Backs the current-price resolution query (filter by Article+Organization+Currency, newest first).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticlePrices_Article_Org_Currency_EffectiveDate')
BEGIN
    CREATE INDEX IX_ArticlePrices_Article_Org_Currency_EffectiveDate
        ON ArticlePrices (ArticleId, OrganizationId, CurrencyCode, EffectiveDate DESC)
        INCLUDE (Price);
END
GO

-- FK lookups/joins — SQL Server does not auto-index foreign keys.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ArticlePrices_OrganizationId')
BEGIN
    CREATE INDEX IX_ArticlePrices_OrganizationId ON ArticlePrices (OrganizationId);
END
GO

-- Prevents two ambiguous "current price" candidates for the same global price on the same date.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ArticlePrices_Global')
BEGIN
    CREATE UNIQUE INDEX UX_ArticlePrices_Global
        ON ArticlePrices (ArticleId, CurrencyCode, EffectiveDate)
        WHERE OrganizationId IS NULL;
END
GO

-- Same guarantee for contract (organization-specific) prices.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ArticlePrices_Contract')
BEGIN
    CREATE UNIQUE INDEX UX_ArticlePrices_Contract
        ON ArticlePrices (ArticleId, OrganizationId, CurrencyCode, EffectiveDate)
        WHERE OrganizationId IS NOT NULL;
END
GO
