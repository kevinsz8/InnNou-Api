-- =============================================================
-- MIGRATION: Create Par Levels module (ParLevelOverrideTypes,
--            ParLevels, ParLevelOverrides)
-- Date: 2026-07-28
-- =============================================================
-- TODO.md item #1 ("Niveles de par / reposicion sugerida"). A par level is
-- a minimum stock threshold + reorder quantity per (Warehouse, Article) --
-- when on-hand stock drops below the minimum it surfaces on a "below par"
-- list for a human to act on (never auto-creates an Order, same "suggest,
-- dont auto-execute" philosophy as Order Templates).
--
-- Two layers on top of the base ParLevels row:
--   - SEASONAL override: a month/day range that repeats every year (not
--     tied to a specific year), including wrap-around across the year
--     boundary (e.g. Dec 20 -> Jan 6).
--   - EVENT override: a one-off literal calendar date range, entered ad
--     hoc when a specific event (wedding, conference) is booked.
-- Both share one ParLevelOverrides table, discriminated by
-- ParLevelOverrideTypeId -- mirrors the GoodsReceiptLine precedent
-- (LotNumber/ExpirationDate/SerialNumber: nullable columns on one table,
-- populated conditionally, validated in C#, not split into per-type
-- tables). Resolution priority when both could apply on a given day:
-- EVENT > SEASONAL > base ParLevels row.
--
-- LeadTimeDays awareness is deliberately NOT computed here (no consumption-
-- rate/demand data exists anywhere in this codebase yet -- see
-- .claude/InventoryModule.md) -- Article.LeadTimeDays is surfaced as-is by
-- the C# service alongside the below-par list, never turned into a
-- fabricated "days until stockout" score.
--
-- Idempotent -- safe to re-run.
-- =============================================================

IF OBJECT_ID('ParLevelOverrideTypes', 'U') IS NULL
BEGIN
    CREATE TABLE ParLevelOverrideTypes (
        ParLevelOverrideTypeId int         NOT NULL IDENTITY(1,1),
        Code                   varchar(20) NOT NULL,
        IsActive               bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_ParLevelOverrideTypes PRIMARY KEY (ParLevelOverrideTypeId),
        CONSTRAINT UQ_ParLevelOverrideTypes_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters -- the C# ParLevelOverrideType enum hardcodes these Ids.
IF NOT EXISTS (SELECT 1 FROM ParLevelOverrideTypes WHERE Code = 'SEASONAL')
    INSERT INTO ParLevelOverrideTypes (Code) VALUES ('SEASONAL');
GO
IF NOT EXISTS (SELECT 1 FROM ParLevelOverrideTypes WHERE Code = 'EVENT')
    INSERT INTO ParLevelOverrideTypes (Code) VALUES ('EVENT');
GO

IF OBJECT_ID('ParLevels', 'U') IS NULL
BEGIN
    CREATE TABLE ParLevels (
        ParLevelId       int              NOT NULL IDENTITY(1,1),
        ParLevelToken    uniqueidentifier NOT NULL DEFAULT NEWID(),
        WarehouseId      int              NOT NULL,
        ArticleId        int              NOT NULL,

        MinimumQuantity  decimal(18,4)    NOT NULL,   -- denominated in Article.PurchaseUnitId, same unit as StockLevels
        ReorderQuantity  decimal(18,4)    NOT NULL,   -- amount to add when replenishing, not a target level

        CreatedUtc       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy        varchar(150)     NOT NULL,
        LastUpdatedUtc   datetime2            NULL,
        LastUpdatedBy    varchar(150)         NULL,

        CONSTRAINT PK_ParLevels PRIMARY KEY (ParLevelId),
        CONSTRAINT FK_ParLevels_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_ParLevels_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT CK_ParLevels_MinimumNonNegative CHECK (MinimumQuantity >= 0),
        CONSTRAINT CK_ParLevels_ReorderPositive CHECK (ReorderQuantity > 0)
    );

    CREATE UNIQUE INDEX UQ_ParLevels_ParLevelToken ON ParLevels (ParLevelToken);

    -- One base row per Warehouse x Article -- Create rejects if one already exists
    -- (PAR_LEVEL_ALREADY_EXISTS), Edit is the path to change it.
    CREATE UNIQUE INDEX UX_ParLevels_Warehouse_Article ON ParLevels (WarehouseId, ArticleId);
END
GO

IF OBJECT_ID('ParLevelOverrides', 'U') IS NULL
BEGIN
    CREATE TABLE ParLevelOverrides (
        ParLevelOverrideId      int              NOT NULL IDENTITY(1,1),
        ParLevelOverrideToken   uniqueidentifier NOT NULL DEFAULT NEWID(),
        WarehouseId             int              NOT NULL,
        ArticleId               int              NOT NULL,
        ParLevelOverrideTypeId  int              NOT NULL,

        Label                   nvarchar(200)        NULL,   -- e.g. "Temporada alta", "Boda Martinez" -- human-facing only

        MinimumQuantity         decimal(18,4)    NOT NULL,
        ReorderQuantity         decimal(18,4)    NOT NULL,

        -- SEASONAL only (recurring, no year)
        StartMonth              tinyint              NULL,
        StartDay                tinyint              NULL,
        EndMonth                tinyint              NULL,
        EndDay                  tinyint              NULL,

        -- EVENT only (literal calendar dates, one-off)
        StartDate               date                 NULL,
        EndDate                 date                 NULL,

        CreatedUtc              datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy               varchar(150)     NOT NULL,

        CONSTRAINT PK_ParLevelOverrides PRIMARY KEY (ParLevelOverrideId),
        CONSTRAINT FK_ParLevelOverrides_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_ParLevelOverrides_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT FK_ParLevelOverrides_Types FOREIGN KEY (ParLevelOverrideTypeId) REFERENCES ParLevelOverrideTypes (ParLevelOverrideTypeId),
        CONSTRAINT CK_ParLevelOverrides_MinimumNonNegative CHECK (MinimumQuantity >= 0),
        CONSTRAINT CK_ParLevelOverrides_ReorderPositive CHECK (ReorderQuantity > 0),

        -- Type=1 (SEASONAL) / Type=2 (EVENT) hardcoded literals, mirrors
        -- InventoryMovementType's own hardcoded-seed-Id precedent. Validates
        -- shape only (right column group populated, 1..12/1..31 bounds,
        -- StartDate <= EndDate) -- full calendar validity (e.g. rejecting
        -- EndDay=31 on a 30-day month, and Feb 29 entirely) is deliberately
        -- left to C#, same "SPs stay dumb" division of labor as everywhere
        -- else in this codebase.
        CONSTRAINT CK_ParLevelOverrides_TypeColumns CHECK (
            (ParLevelOverrideTypeId = 1
                AND StartMonth BETWEEN 1 AND 12 AND EndMonth BETWEEN 1 AND 12
                AND StartDay BETWEEN 1 AND 31 AND EndDay BETWEEN 1 AND 31
                AND StartDate IS NULL AND EndDate IS NULL)
            OR
            (ParLevelOverrideTypeId = 2
                AND StartDate IS NOT NULL AND EndDate IS NOT NULL AND StartDate <= EndDate
                AND StartMonth IS NULL AND StartDay IS NULL AND EndMonth IS NULL AND EndDay IS NULL)
        )
    );

    CREATE UNIQUE INDEX UQ_ParLevelOverrides_ParLevelOverrideToken ON ParLevelOverrides (ParLevelOverrideToken);

    -- Supports both the C#-side overlap check (fetch same-type candidates for a
    -- Warehouse+Article) and the resolution query's OUTER APPLY.
    CREATE INDEX IX_ParLevelOverrides_Warehouse_Article_Type ON ParLevelOverrides (WarehouseId, ArticleId, ParLevelOverrideTypeId);
END
GO

PRINT '=== Migration 20260728_ParLevels_Create completed successfully ===';
GO
