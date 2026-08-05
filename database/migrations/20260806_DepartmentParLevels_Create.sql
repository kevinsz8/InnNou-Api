-- =============================================================
-- MIGRATION: Create Department Par Levels (DepartmentParLevels)
-- Date: 2026-08-06
-- =============================================================
-- Value-add on top of Requisiciones Internas (see .claude/RequisitionsModule.md):
-- a per-(Department, Article) minimum + reorder quantity, mirroring the shape of
-- the existing Warehouse-level ParLevels (see 20260728_ParLevels_Create.sql), so a
-- Department can configure "how much of this article we should keep flowing" and
-- get it surfaced on a "Sugeridas" tab instead of having to remember to re-request.
--
-- Deliberately V1-scoped narrower than Warehouse ParLevels: no Seasonal/Event
-- override layer (ParLevelOverrides' own equivalent) -- confirmed as a reasonable
-- first cut; can be added later the same way ParLevelOverrides was layered onto
-- the base ParLevels row, without a schema change to this table.
--
-- No WarehouseId on this table, unlike ParLevels -- a Department's own "how much
-- do we need on hand" policy doesn't depend on which Warehouse currently fulfills
-- it (that's a per-Requisition fulfillment choice, not a standing configuration).
--
-- Critical difference from Warehouse ParLevels' "below par" resolution: a
-- Department has no StockLevels of its own to compare MinimumQuantity against --
-- Requisiciones only track what's been issued OUT, never what the Department
-- still has on hand (no department-level inventory ledger exists or is planned).
-- "Suggested" is therefore resolved from real CONSUMPTION history (see
-- InventoryMovements/RequisitionIssueLines) as a consumption-pace/time-elapsed
-- signal, never a fabricated live balance -- see sp_DepartmentParLevel_GetSuggested
-- and .claude/RequisitionsModule.md for the exact formula and reasoning.
--
-- Idempotent -- safe to re-run.
-- =============================================================

IF OBJECT_ID('DepartmentParLevels', 'U') IS NULL
BEGIN
    CREATE TABLE DepartmentParLevels (
        DepartmentParLevelId    int              NOT NULL IDENTITY(1,1),
        DepartmentParLevelToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        DepartmentId            int              NOT NULL,
        ArticleId               int              NOT NULL,

        MinimumQuantity         decimal(18,8)    NOT NULL,   -- denominated in Article.PurchaseUnitId; DECIMAL(18,8) to match the 2026-08-04 codebase-wide money/qty precision widening
        ReorderQuantity         decimal(18,8)    NOT NULL,   -- suggested quantity to request, not a target level

        IsActive                bit              NOT NULL DEFAULT 1,

        CreatedUtc              datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy               varchar(150)     NOT NULL,
        LastUpdatedUtc          datetime2            NULL,
        LastUpdatedBy           varchar(150)         NULL,

        CONSTRAINT PK_DepartmentParLevels PRIMARY KEY (DepartmentParLevelId),
        CONSTRAINT FK_DepartmentParLevels_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments (DepartmentId),
        CONSTRAINT FK_DepartmentParLevels_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT CK_DepartmentParLevels_MinimumNonNegative CHECK (MinimumQuantity >= 0),
        CONSTRAINT CK_DepartmentParLevels_ReorderPositive CHECK (ReorderQuantity > 0)
    );

    CREATE UNIQUE INDEX UQ_DepartmentParLevels_Token ON DepartmentParLevels (DepartmentParLevelToken);

    -- One base row per Department x Article -- Create rejects if one already exists
    -- (DEPARTMENT_PAR_LEVEL_ALREADY_EXISTS), Edit is the path to change it.
    CREATE UNIQUE INDEX UX_DepartmentParLevels_Department_Article ON DepartmentParLevels (DepartmentId, ArticleId);
END
GO

PRINT '=== Migration 20260806_DepartmentParLevels_Create completed successfully ===';
GO
