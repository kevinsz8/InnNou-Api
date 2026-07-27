-- =============================================================
-- MIGRATION: Create Inventory Periods (state-machine counting periods)
-- Date: 2026-07-29
-- =============================================================
-- Closes the reporting gap flagged in InventoryModule.md: StockLevels is a
-- single always-current balance with no reconciliation checkpoint, so "what
-- did we have on a given cut-off" can't be answered cheaply. Researched
-- against SAP (MI01 create / MI04 count / MI07 post differences), Dynamics
-- 365 cycle counting (Open -> In process -> Pending review -> Closed) and
-- BirchStreet/Adaco's flexible "Period Inventory" (operator picks the
-- cadence per warehouse — monthly/quarterly/etc, not a system-fixed
-- calendar). Landed on a state machine, not a date range: the close is
-- always "now", so no "freeze writes before date X" mechanism is needed.
--
-- OPEN -> IN_PROGRESS -> PRE_CLOSED are auto-computed from count
-- completeness; CLOSED only happens via an explicit confirm action, which
-- is when variance-driven ADJUSTMENT movements are actually posted (reusing
-- sp_StockLevel_ApplyDelta/sp_InventoryMovement_Create as-is, no new
-- movement type). OpeningQuantity is a live StockLevels snapshot taken at
-- open time — since the ledger never resets between periods, this already
-- equals the prior period's closing quantity for free (the classic
-- "opening = previous closing" restaurant/bar inventory practice).
--
-- At most one non-CLOSED period per warehouse at a time (filtered unique
-- index) — this is what gives continuity/no-overlap for free, no C#
-- date-range validation needed.
--
-- Idempotent — safe to re-run.
-- =============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Warehouses', 'CanCountInventory') IS NULL
    ALTER TABLE Warehouses ADD CanCountInventory BIT NOT NULL DEFAULT (0);
GO

IF OBJECT_ID('InventoryPeriodStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryPeriodStatuses (
        InventoryPeriodStatusId int         NOT NULL IDENTITY(1,1),
        Code                    varchar(20) NOT NULL,
        IsActive                bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_InventoryPeriodStatuses PRIMARY KEY (InventoryPeriodStatusId),
        CONSTRAINT UQ_InventoryPeriodStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# InventoryPeriodStatus enum hardcodes these Ids.
IF NOT EXISTS (SELECT 1 FROM InventoryPeriodStatuses WHERE Code = 'OPEN')
    INSERT INTO InventoryPeriodStatuses (Code) VALUES ('OPEN');
GO
IF NOT EXISTS (SELECT 1 FROM InventoryPeriodStatuses WHERE Code = 'IN_PROGRESS')
    INSERT INTO InventoryPeriodStatuses (Code) VALUES ('IN_PROGRESS');
GO
IF NOT EXISTS (SELECT 1 FROM InventoryPeriodStatuses WHERE Code = 'PRE_CLOSED')
    INSERT INTO InventoryPeriodStatuses (Code) VALUES ('PRE_CLOSED');
GO
IF NOT EXISTS (SELECT 1 FROM InventoryPeriodStatuses WHERE Code = 'CLOSED')
    INSERT INTO InventoryPeriodStatuses (Code) VALUES ('CLOSED');
GO

IF OBJECT_ID('InventoryPeriods', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryPeriods (
        InventoryPeriodId       int              NOT NULL IDENTITY(1,1),
        InventoryPeriodToken    uniqueidentifier NOT NULL DEFAULT NEWID(),
        WarehouseId             int              NOT NULL,
        InventoryPeriodStatusId int              NOT NULL DEFAULT (1),   -- OPEN

        StartDate               datetime2        NOT NULL,

        ClosedUtc               datetime2            NULL,
        ClosedBy                varchar(150)         NULL,
        ReopenedUtc             datetime2            NULL,
        ReopenedBy              varchar(150)         NULL,

        Notes                   nvarchar(1000)       NULL,

        CreatedUtc              datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy               varchar(150)     NOT NULL,
        LastUpdatedUtc          datetime2            NULL,
        LastUpdatedBy           varchar(150)         NULL,

        CONSTRAINT PK_InventoryPeriods PRIMARY KEY (InventoryPeriodId),
        CONSTRAINT FK_InventoryPeriods_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_InventoryPeriods_InventoryPeriodStatuses FOREIGN KEY (InventoryPeriodStatusId) REFERENCES InventoryPeriodStatuses (InventoryPeriodStatusId)
    );

    CREATE UNIQUE INDEX UQ_InventoryPeriods_InventoryPeriodToken ON InventoryPeriods (InventoryPeriodToken);

    -- At most one non-CLOSED (id 4) period per warehouse — this alone gives
    -- continuity/no-overlap, no date-range validation needed anywhere.
    CREATE UNIQUE INDEX UX_InventoryPeriods_OneActivePerWarehouse ON InventoryPeriods (WarehouseId) WHERE InventoryPeriodStatusId <> 4;

    CREATE INDEX IX_InventoryPeriods_WarehouseId ON InventoryPeriods (WarehouseId);
END
GO

IF OBJECT_ID('InventoryPeriodCounts', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryPeriodCounts (
        InventoryPeriodCountId    int              NOT NULL IDENTITY(1,1),
        InventoryPeriodCountToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        InventoryPeriodId         int              NOT NULL,
        ArticleId                 int              NOT NULL,

        OpeningQuantity           decimal(18,4)    NOT NULL,   -- live StockLevels snapshot at period-open time
        CountedQuantity           decimal(18,4)        NULL,   -- NULL = not yet counted
        SystemQuantityAtClose     decimal(18,4)        NULL,   -- populated only at close time
        VarianceQuantity          decimal(18,4)        NULL,   -- CountedQuantity - SystemQuantityAtClose, at close time

        CreatedUtc                datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                 varchar(150)     NOT NULL,
        LastUpdatedUtc            datetime2            NULL,
        LastUpdatedBy             varchar(150)         NULL,

        CONSTRAINT PK_InventoryPeriodCounts PRIMARY KEY (InventoryPeriodCountId),
        CONSTRAINT FK_InventoryPeriodCounts_InventoryPeriods FOREIGN KEY (InventoryPeriodId) REFERENCES InventoryPeriods (InventoryPeriodId),
        CONSTRAINT FK_InventoryPeriodCounts_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId)
    );

    CREATE UNIQUE INDEX UQ_InventoryPeriodCounts_InventoryPeriodCountToken ON InventoryPeriodCounts (InventoryPeriodCountToken);

    -- One line per Article per Period — periods are seeded complete at open time.
    CREATE UNIQUE INDEX UX_InventoryPeriodCounts_Period_Article ON InventoryPeriodCounts (InventoryPeriodId, ArticleId);
END
GO

IF COL_LENGTH('dbo.InventoryMovements', 'InventoryPeriodCountId') IS NULL
    ALTER TABLE InventoryMovements ADD InventoryPeriodCountId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_InventoryMovements_InventoryPeriodCounts')
    ALTER TABLE InventoryMovements ADD CONSTRAINT FK_InventoryMovements_InventoryPeriodCounts
        FOREIGN KEY (InventoryPeriodCountId) REFERENCES InventoryPeriodCounts (InventoryPeriodCountId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryMovements_InventoryPeriodCountId' AND object_id = OBJECT_ID('dbo.InventoryMovements'))
    CREATE INDEX IX_InventoryMovements_InventoryPeriodCountId ON InventoryMovements (InventoryPeriodCountId);
GO

PRINT '=== Migration 20260729_InventoryPeriods_Create completed successfully ===';
GO
