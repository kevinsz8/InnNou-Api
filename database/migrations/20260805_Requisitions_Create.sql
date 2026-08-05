SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- MIGRATION: Create Requisitions module ("Requisiciones internas" /
--            Store Requisition — issue stock to a Department)
-- Date: 2026-08-05
-- =============================================================
-- Closes the gap flagged in CLAUDE.md's Par Levels ("no CONSUMPTION
-- movement type exists") and TODO.md item #4 (recipe costing needs a real
-- consumption ledger to book against) — the first real "stock going out for
-- an operational reason, not a sale" flow in InnNou.
--
-- Researched before building (Oracle Hospitality Materials Control,
-- BirchStreet, Adaco, SAP MM's "Goods Issue to Cost Center"): every
-- hospitality-specific system treats an internal store requisition (a
-- Department pulling stock from a central store — Housekeeping, Engineering,
-- Spa, Banquets, none of which ever touch a POS) as foundational and
-- separate from POS-driven consumption, which only ever covers F&B and is
-- layered on top later, never the reverse. POS integration is deliberately
-- out of scope here — no current driver, and it would just become another
-- writer into the same CONSUMPTION ledger this migration establishes.
--
-- V1 design decisions confirmed with the user:
-- - Departments are per-Organization (each Asociado defines its own), same
--   shape as Warehouses — not a shared global catalog.
-- - 3-state approval flow: REQUESTED -> APPROVED -> (PARTIALLY_ISSUED ->)
--   ISSUED, with a review gate before stock physically moves — deliberately
--   NOT reusing OrderApprovalStep's spend-threshold machinery, this is a
--   much lighter governance need (low-value operational items, not big
--   purchases).
-- - Issuance is a repeatable, append-only sub-document (RequisitionIssues +
--   RequisitionIssueLines), same "freeze the request, append fulfillment
--   over time" shape as PurchaseOrderLine -> GoodsReceiptLine and
--   InternalOrderLine -> InternalOrderShipmentLine/InternalOrderReceiptLine
--   — a store may not have full stock on hand today and issue the rest
--   later. CLOSED_SHORT (mirrors PurchaseOrder's own 2026-07-28 addition)
--   closes out a requisition that will never be fully issued, without
--   mutating RequisitionLines/QuantityRequested history.
-- - No "Department contact" shadow-user concept — any Staff+ user in the
--   organization creates a Requisition and simply picks which Department
--   it's for from a dropdown, same reasoning Orders/Inventory already
--   established (Warehouse is a resource reference, not something you have
--   to be — the equivalent applies to Department here).
-- - Own domain end-to-end (own tables, own IRequisitionService) rather than
--   folding into InventoryService — it has its own approval+issuance
--   lifecycle, closer in shape to Internal Orders than to a GoodsReceipt.
--
-- Idempotent — safe to re-run.
-- =============================================================

-- ── Departments (per-Organization, mirrors Warehouses' own Name/NormalizedName shape) ──
IF OBJECT_ID('Departments', 'U') IS NULL
BEGIN
    CREATE TABLE Departments (
        DepartmentId     int              NOT NULL IDENTITY(1,1),
        DepartmentToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
        OrganizationId   int              NOT NULL,
        Name             nvarchar(150)    NOT NULL,
        NormalizedName   nvarchar(150)    NOT NULL,
        Code             varchar(20)          NULL,

        IsActive         bit              NOT NULL DEFAULT (1),

        CreatedUtc       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy        varchar(150)     NOT NULL,
        LastUpdatedUtc   datetime2            NULL,
        LastUpdatedBy    varchar(150)         NULL,

        CONSTRAINT PK_Departments PRIMARY KEY (DepartmentId),
        CONSTRAINT FK_Departments_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
    );

    CREATE UNIQUE INDEX UQ_Departments_DepartmentToken ON Departments (DepartmentToken);
    CREATE INDEX IX_Departments_OrganizationId ON Departments (OrganizationId);
END
GO

-- ── RequisitionStatuses (lookup) ────────────────────────────────────────
IF OBJECT_ID('RequisitionStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE RequisitionStatuses (
        RequisitionStatusId int         NOT NULL IDENTITY(1,1),
        Code                 varchar(20) NOT NULL,
        IsActive              bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_RequisitionStatuses PRIMARY KEY (RequisitionStatusId),
        CONSTRAINT UQ_RequisitionStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# RequisitionStatus enum hardcodes these Ids.
IF NOT EXISTS (SELECT 1 FROM RequisitionStatuses WHERE Code = 'REQUESTED')
    INSERT INTO RequisitionStatuses (Code) VALUES ('REQUESTED');
GO
IF NOT EXISTS (SELECT 1 FROM RequisitionStatuses WHERE Code = 'APPROVED')
    INSERT INTO RequisitionStatuses (Code) VALUES ('APPROVED');
GO
IF NOT EXISTS (SELECT 1 FROM RequisitionStatuses WHERE Code = 'PARTIALLY_ISSUED')
    INSERT INTO RequisitionStatuses (Code) VALUES ('PARTIALLY_ISSUED');
GO
IF NOT EXISTS (SELECT 1 FROM RequisitionStatuses WHERE Code = 'ISSUED')
    INSERT INTO RequisitionStatuses (Code) VALUES ('ISSUED');
GO
IF NOT EXISTS (SELECT 1 FROM RequisitionStatuses WHERE Code = 'REJECTED')
    INSERT INTO RequisitionStatuses (Code) VALUES ('REJECTED');
GO
IF NOT EXISTS (SELECT 1 FROM RequisitionStatuses WHERE Code = 'CANCELLED')
    INSERT INTO RequisitionStatuses (Code) VALUES ('CANCELLED');
GO
IF NOT EXISTS (SELECT 1 FROM RequisitionStatuses WHERE Code = 'CLOSED_SHORT')
    INSERT INTO RequisitionStatuses (Code) VALUES ('CLOSED_SHORT');
GO

-- ── RequisitionNumberCounters (sequential numbering, same recipe as PurchaseOrderNumberCounters/InternalOrderNumberCounters) ──
IF OBJECT_ID('RequisitionNumberCounters', 'U') IS NULL
BEGIN
    CREATE TABLE RequisitionNumberCounters (
        RequisitionNumberCounterId int NOT NULL IDENTITY(1,1),
        OrganizationId              int NOT NULL,
        Year                        int NOT NULL,
        LastNumber                  int NOT NULL DEFAULT (0),

        CONSTRAINT PK_RequisitionNumberCounters PRIMARY KEY (RequisitionNumberCounterId),
        CONSTRAINT FK_RequisitionNumberCounters_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
    );

    CREATE UNIQUE INDEX UX_RequisitionNumberCounters_Org_Year ON RequisitionNumberCounters (OrganizationId, Year);
END
GO

-- ── Requisitions (header) ───────────────────────────────────────────────
IF OBJECT_ID('Requisitions', 'U') IS NULL
BEGIN
    CREATE TABLE Requisitions (
        RequisitionId       int              NOT NULL IDENTITY(1,1),
        RequisitionToken    uniqueidentifier NOT NULL DEFAULT NEWID(),
        RequisitionNumber   varchar(20)      NOT NULL,   -- REQ-{Year}-{5-digit}, e.g. REQ-2026-00042

        OrganizationId       int              NOT NULL,
        WarehouseId          int              NOT NULL,   -- the store being requested FROM
        DepartmentId         int              NOT NULL,   -- who's requesting

        RequisitionStatusId  int              NOT NULL DEFAULT (1),  -- 1 = REQUESTED

        Notes                nvarchar(1000)       NULL,

        ApprovedUtc          datetime2            NULL,
        ApprovedBy           varchar(150)         NULL,

        RejectedUtc          datetime2            NULL,
        RejectedBy           varchar(150)         NULL,
        RejectedReason       nvarchar(500)        NULL,

        CancelledUtc         datetime2            NULL,
        CancelledBy          varchar(150)         NULL,
        CancelledReason      nvarchar(500)        NULL,

        ClosedShortUtc       datetime2            NULL,
        ClosedShortBy        varchar(150)         NULL,
        ClosedShortReason    nvarchar(500)        NULL,

        CreatedUtc           datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy            varchar(150)     NOT NULL,
        LastUpdatedUtc        datetime2            NULL,
        LastUpdatedBy         varchar(150)         NULL,

        CONSTRAINT PK_Requisitions PRIMARY KEY (RequisitionId),
        CONSTRAINT FK_Requisitions_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_Requisitions_Warehouses FOREIGN KEY (WarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_Requisitions_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments (DepartmentId),
        CONSTRAINT FK_Requisitions_RequisitionStatuses FOREIGN KEY (RequisitionStatusId) REFERENCES RequisitionStatuses (RequisitionStatusId)
    );

    CREATE UNIQUE INDEX UQ_Requisitions_RequisitionToken ON Requisitions (RequisitionToken);
    CREATE UNIQUE INDEX UX_Requisitions_Org_Number ON Requisitions (OrganizationId, RequisitionNumber);
    CREATE INDEX IX_Requisitions_WarehouseId ON Requisitions (WarehouseId);
    CREATE INDEX IX_Requisitions_DepartmentId ON Requisitions (DepartmentId);
    CREATE INDEX IX_Requisitions_RequisitionStatusId ON Requisitions (RequisitionStatusId);
END
GO

-- ── RequisitionLines — the firm request, immutable once created (never mutated after,
--    same convention as PurchaseOrderLine/InternalOrderLine) ───────────────
IF OBJECT_ID('RequisitionLines', 'U') IS NULL
BEGIN
    CREATE TABLE RequisitionLines (
        RequisitionLineId     int              NOT NULL IDENTITY(1,1),
        RequisitionLineToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
        RequisitionId          int              NOT NULL,
        ArticleId               int              NOT NULL,

        QuantityRequested       decimal(18,8)    NOT NULL,   -- PurchaseUnitId-denominated, same as every other line quantity

        Notes                    nvarchar(500)        NULL,

        CreatedUtc               datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                varchar(150)     NOT NULL,

        CONSTRAINT PK_RequisitionLines PRIMARY KEY (RequisitionLineId),
        CONSTRAINT FK_RequisitionLines_Requisitions FOREIGN KEY (RequisitionId) REFERENCES Requisitions (RequisitionId),
        CONSTRAINT FK_RequisitionLines_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT CK_RequisitionLines_QuantityPositive CHECK (QuantityRequested > 0)
    );

    CREATE UNIQUE INDEX UQ_RequisitionLines_RequisitionLineToken ON RequisitionLines (RequisitionLineToken);
    CREATE INDEX IX_RequisitionLines_RequisitionId ON RequisitionLines (RequisitionId);
END
GO

-- ── RequisitionIssues (header) — the store's fulfillment event(s). Repeatable: a Requisition
--    can be issued in more than one batch over time (stock-shortage-today, issue-the-rest-later
--    — same reason GoodsReceipt/InternalOrderShipment allow it). ──────────
IF OBJECT_ID('RequisitionIssues', 'U') IS NULL
BEGIN
    CREATE TABLE RequisitionIssues (
        RequisitionIssueId     int              NOT NULL IDENTITY(1,1),
        RequisitionIssueToken  uniqueidentifier NOT NULL DEFAULT NEWID(),
        RequisitionId           int              NOT NULL,

        Notes                    nvarchar(1000)       NULL,

        CreatedUtc               datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                varchar(150)     NOT NULL,

        CONSTRAINT PK_RequisitionIssues PRIMARY KEY (RequisitionIssueId),
        CONSTRAINT FK_RequisitionIssues_Requisitions FOREIGN KEY (RequisitionId) REFERENCES Requisitions (RequisitionId)
    );

    CREATE UNIQUE INDEX UQ_RequisitionIssues_Token ON RequisitionIssues (RequisitionIssueToken);
    CREATE INDEX IX_RequisitionIssues_RequisitionId ON RequisitionIssues (RequisitionId);
END
GO

IF OBJECT_ID('RequisitionIssueLines', 'U') IS NULL
BEGIN
    CREATE TABLE RequisitionIssueLines (
        RequisitionIssueLineId    int              NOT NULL IDENTITY(1,1),
        RequisitionIssueLineToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        RequisitionIssueId         int              NOT NULL,
        RequisitionLineId           int              NOT NULL,   -- closes out against the original request directly, no shipment step

        QuantityIssued               decimal(18,8)    NOT NULL,

        Notes                         nvarchar(500)        NULL,

        CreatedUtc                    datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                     varchar(150)     NOT NULL,

        CONSTRAINT PK_RequisitionIssueLines PRIMARY KEY (RequisitionIssueLineId),
        CONSTRAINT FK_RequisitionIssueLines_RequisitionIssues FOREIGN KEY (RequisitionIssueId) REFERENCES RequisitionIssues (RequisitionIssueId),
        CONSTRAINT FK_RequisitionIssueLines_RequisitionLines FOREIGN KEY (RequisitionLineId) REFERENCES RequisitionLines (RequisitionLineId),
        CONSTRAINT CK_RequisitionIssueLines_QuantityPositive CHECK (QuantityIssued > 0)
    );

    CREATE UNIQUE INDEX UQ_RequisitionIssueLines_Token ON RequisitionIssueLines (RequisitionIssueLineToken);
    CREATE INDEX IX_RequisitionIssueLines_IssueId ON RequisitionIssueLines (RequisitionIssueId);
    CREATE INDEX IX_RequisitionIssueLines_LineId ON RequisitionIssueLines (RequisitionLineId);
END
GO

-- ── InventoryMovementTypes: 1 new Code for this module's own stock effect (kept distinct from
--    RECEIPT/ADJUSTMENT/TRANSFER_OUT/TRANSFER_IN/INTERNAL_ORDER_* so the audit trail always shows
--    the real cause). Always a negative Quantity delta — stock leaving for departmental use. ──
IF NOT EXISTS (SELECT 1 FROM InventoryMovementTypes WHERE Code = 'CONSUMPTION')
    INSERT INTO InventoryMovementTypes (Code) VALUES ('CONSUMPTION');
GO

-- InventoryMovements gains 1 more nullable "origin" FK column, same shape as its existing
-- GoodsReceiptLineId/InventoryTransferLineId/InternalOrderShipmentLineId/InternalOrderReceiptLineId
-- columns — exactly one of the (now five) origin columns is ever set per row, depending on
-- InventoryMovementTypeId.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InventoryMovements') AND name = 'RequisitionIssueLineId')
    ALTER TABLE InventoryMovements ADD RequisitionIssueLineId int NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_InventoryMovements_RequisitionIssueLines')
    ALTER TABLE InventoryMovements ADD CONSTRAINT FK_InventoryMovements_RequisitionIssueLines FOREIGN KEY (RequisitionIssueLineId) REFERENCES RequisitionIssueLines (RequisitionIssueLineId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryMovements_RequisitionIssueLineId' AND object_id = OBJECT_ID('InventoryMovements'))
    CREATE INDEX IX_InventoryMovements_RequisitionIssueLineId ON InventoryMovements (RequisitionIssueLineId);
GO

-- ── Warehouses gains its 18th capability bit — a store must be explicitly configured to issue to
--    departments, same "capability column, not a Name check" convention as every other one. ──
IF COL_LENGTH('dbo.Warehouses', 'CanIssueToDepartment') IS NULL
    ALTER TABLE Warehouses ADD CanIssueToDepartment BIT NOT NULL DEFAULT (0);
GO

PRINT '=== Migration 20260805_Requisitions_Create completed successfully ===';
GO
