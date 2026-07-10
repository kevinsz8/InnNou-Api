-- =============================================================
-- MIGRATION: Create Currencies table
-- Date: 2026-07-10
-- =============================================================
-- Read-only, global, code-based reference catalog (ISO 4217) — same
-- "system data only, no audit fields" shape as UnitTypes/Families/
-- Categories. No CRUD: ArticlePrices.CurrencyCode already references
-- currencies by raw code string, not FK, so this table exists purely
-- to back a validated dropdown and an existence check on write.
-- Guarded so it is a no-op if already applied.
-- =============================================================

IF OBJECT_ID('Currencies', 'U') IS NULL
BEGIN
    CREATE TABLE Currencies
    (
        CurrencyId INT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code       VARCHAR(3)   NOT NULL UNIQUE,   -- ISO 4217, e.g. EUR, USD
        IsActive   BIT          NOT NULL DEFAULT 1
    );
END
GO
