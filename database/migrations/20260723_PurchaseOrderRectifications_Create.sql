SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: PurchaseOrderRectifications (post-send corrections to a
   SENT PurchaseOrder — "rectificacion de pedido")

   A rectification corrects a PurchaseOrderLine's Quantity/UnitPrice/
   CurrencyCode, or cancels it outright, AFTER the PurchaseOrder has
   already been sent to the supplier — distinct from Goods Receipts
   (what physically arrived) and from a fiscally-regulated Factura
   Rectificativa (corrects an already-issued invoice). See
   .claude/PurchaseOrderRectificationModule.md.

   Append-only, ArticlePrice-style: PurchaseOrderLine itself is NEVER
   mutated. PurchaseOrderLineRectifications is an insert-only per-line
   delta log; the "effective" value of a line is always the latest row
   belonging to an APPLIED header, resolved at read time (see
   sp_PurchaseOrderLine_GetEffective), falling back to the original
   PurchaseOrderLine snapshot when no rectification has ever applied.

   A rectification that raises a Family's spend total for the whole
   originating Order past a not-yet-approved FamilyApprovalThreshold
   level reuses OrderApprovalSteps (TriggeringPurchaseOrderRectificationId
   tags which rows came from a rectification instead of the original
   Submit) — same approver/threshold config, same sequential-level gate,
   same "pending approvals" list, no new approval subsystem needed. A
   rectification whose lines don't cross a new level applies immediately.

   Built directly Id-backed (lookup tables from the start) per CLAUDE.md's
   "Status/type fields are Id-backed" convention — never CHECK-constrained
   varchar, since this is a new column/table, not a retrofit.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrderRectificationReasons')
BEGIN
    CREATE TABLE PurchaseOrderRectificationReasons
    (
        PurchaseOrderRectificationReasonId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code                               VARCHAR(30)       NOT NULL,
        IsActive                           BIT               NOT NULL DEFAULT (1),

        CONSTRAINT UQ_PurchaseOrderRectificationReasons_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# PurchaseOrderRectificationReason enum hardcodes these Ids.
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationReasons WHERE Code = 'SUPPLIER_STOCK_SHORTAGE')
    INSERT INTO PurchaseOrderRectificationReasons (Code) VALUES ('SUPPLIER_STOCK_SHORTAGE');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationReasons WHERE Code = 'PRICE_CORRECTION')
    INSERT INTO PurchaseOrderRectificationReasons (Code) VALUES ('PRICE_CORRECTION');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationReasons WHERE Code = 'QUANTITY_ERROR')
    INSERT INTO PurchaseOrderRectificationReasons (Code) VALUES ('QUANTITY_ERROR');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationReasons WHERE Code = 'DELIVERY_ISSUE')
    INSERT INTO PurchaseOrderRectificationReasons (Code) VALUES ('DELIVERY_ISSUE');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationReasons WHERE Code = 'OTHER')
    INSERT INTO PurchaseOrderRectificationReasons (Code) VALUES ('OTHER');
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrderRectificationStatuses')
BEGIN
    CREATE TABLE PurchaseOrderRectificationStatuses
    (
        PurchaseOrderRectificationStatusId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code                               VARCHAR(20)       NOT NULL,
        IsActive                           BIT               NOT NULL DEFAULT (1),

        CONSTRAINT UQ_PurchaseOrderRectificationStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# PurchaseOrderRectificationStatus enum hardcodes these Ids
-- (PendingApproval=1, Applied=2, Rejected=3).
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationStatuses WHERE Code = 'PENDING_APPROVAL')
    INSERT INTO PurchaseOrderRectificationStatuses (Code) VALUES ('PENDING_APPROVAL');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationStatuses WHERE Code = 'APPLIED')
    INSERT INTO PurchaseOrderRectificationStatuses (Code) VALUES ('APPLIED');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationStatuses WHERE Code = 'REJECTED')
    INSERT INTO PurchaseOrderRectificationStatuses (Code) VALUES ('REJECTED');
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrderRectificationLineActions')
BEGIN
    CREATE TABLE PurchaseOrderRectificationLineActions
    (
        PurchaseOrderRectificationLineActionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code                                   VARCHAR(30)       NOT NULL,
        IsActive                               BIT               NOT NULL DEFAULT (1),

        CONSTRAINT UQ_PurchaseOrderRectificationLineActions_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# PurchaseOrderRectificationLineAction enum hardcodes these Ids
-- (QuantityPriceChange=1, LineCancelled=2).
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationLineActions WHERE Code = 'QUANTITY_PRICE_CHANGE')
    INSERT INTO PurchaseOrderRectificationLineActions (Code) VALUES ('QUANTITY_PRICE_CHANGE');
GO
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderRectificationLineActions WHERE Code = 'LINE_CANCELLED')
    INSERT INTO PurchaseOrderRectificationLineActions (Code) VALUES ('LINE_CANCELLED');
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrderRectifications')
BEGIN
    CREATE TABLE PurchaseOrderRectifications
    (
        PurchaseOrderRectificationId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PurchaseOrderRectificationToken UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWID(),
        PurchaseOrderId                 INT                NOT NULL,
        SequenceNumber                  INT                NOT NULL,
        PurchaseOrderRectificationReasonId INT             NOT NULL,
        Notes                           NVARCHAR(500)      NULL,
        PurchaseOrderRectificationStatusId INT             NOT NULL,
        CreatedUtc                      DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                       VARCHAR(150)       NOT NULL,
        AppliedUtc                      DATETIME2          NULL,

        CONSTRAINT FK_PurchaseOrderRectifications_PurchaseOrder
            FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrder (PurchaseOrderId),
        CONSTRAINT FK_PurchaseOrderRectifications_Reasons
            FOREIGN KEY (PurchaseOrderRectificationReasonId) REFERENCES PurchaseOrderRectificationReasons (PurchaseOrderRectificationReasonId),
        CONSTRAINT FK_PurchaseOrderRectifications_Statuses
            FOREIGN KEY (PurchaseOrderRectificationStatusId) REFERENCES PurchaseOrderRectificationStatuses (PurchaseOrderRectificationStatusId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PurchaseOrderRectifications_PurchaseOrderId_SequenceNumber' AND object_id = OBJECT_ID('PurchaseOrderRectifications'))
BEGIN
    CREATE UNIQUE INDEX UX_PurchaseOrderRectifications_PurchaseOrderId_SequenceNumber ON PurchaseOrderRectifications (PurchaseOrderId, SequenceNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrderLineRectifications')
BEGIN
    CREATE TABLE PurchaseOrderLineRectifications
    (
        PurchaseOrderLineRectificationId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PurchaseOrderLineRectificationToken UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWID(),
        PurchaseOrderRectificationId         INT                NOT NULL,
        PurchaseOrderLineId                  INT                NOT NULL,
        PurchaseOrderRectificationLineActionId INT              NOT NULL,
        PreviousQuantity                     DECIMAL(18,4)      NULL,
        NewQuantity                          DECIMAL(18,4)      NULL,
        PreviousUnitPrice                    DECIMAL(18,4)      NULL,
        NewUnitPrice                         DECIMAL(18,4)      NULL,
        PreviousCurrencyCode                 VARCHAR(3)         NULL,
        NewCurrencyCode                      VARCHAR(3)         NULL,
        CreatedUtc                           DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                            VARCHAR(150)       NOT NULL,

        CONSTRAINT FK_PurchaseOrderLineRectifications_Header
            FOREIGN KEY (PurchaseOrderRectificationId) REFERENCES PurchaseOrderRectifications (PurchaseOrderRectificationId),
        CONSTRAINT FK_PurchaseOrderLineRectifications_PurchaseOrderLine
            FOREIGN KEY (PurchaseOrderLineId) REFERENCES PurchaseOrderLine (PurchaseOrderLineId),
        CONSTRAINT FK_PurchaseOrderLineRectifications_Actions
            FOREIGN KEY (PurchaseOrderRectificationLineActionId) REFERENCES PurchaseOrderRectificationLineActions (PurchaseOrderRectificationLineActionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrderLineRectifications_HeaderId' AND object_id = OBJECT_ID('PurchaseOrderLineRectifications'))
BEGIN
    CREATE INDEX IX_PurchaseOrderLineRectifications_HeaderId ON PurchaseOrderLineRectifications (PurchaseOrderRectificationId);
END
GO

-- The "effective value" resolution query (sp_PurchaseOrderLine_GetEffective) always looks up
-- the latest row per PurchaseOrderLineId ordered by PurchaseOrderLineRectificationId DESC — this
-- index makes that lookup a seek instead of a scan.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrderLineRectifications_LineId' AND object_id = OBJECT_ID('PurchaseOrderLineRectifications'))
BEGIN
    CREATE INDEX IX_PurchaseOrderLineRectifications_LineId ON PurchaseOrderLineRectifications (PurchaseOrderLineId, PurchaseOrderLineRectificationId DESC);
END
GO

-- Tags which OrderApprovalStep rows were created by a rectification's own approval re-evaluation
-- instead of the original Submit — NULL means "from Submit" (unchanged, existing behavior).
-- OrderService.ApproveStepAndAdvanceAsync/RejectOrderApprovalStepAsync branch on this so a
-- rectification's own step batch is scoped independently and never confused with (or allowed to
-- re-trigger) the Order's own submission-completion logic.
IF COL_LENGTH('dbo.OrderApprovalSteps', 'TriggeringPurchaseOrderRectificationId') IS NULL
    ALTER TABLE OrderApprovalSteps ADD TriggeringPurchaseOrderRectificationId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderApprovalSteps_PurchaseOrderRectifications')
    ALTER TABLE OrderApprovalSteps ADD CONSTRAINT FK_OrderApprovalSteps_PurchaseOrderRectifications
        FOREIGN KEY (TriggeringPurchaseOrderRectificationId) REFERENCES PurchaseOrderRectifications (PurchaseOrderRectificationId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderApprovalSteps_TriggeringPurchaseOrderRectificationId')
    CREATE INDEX IX_OrderApprovalSteps_TriggeringPurchaseOrderRectificationId ON OrderApprovalSteps (TriggeringPurchaseOrderRectificationId);
GO

PRINT '=== Migration 20260723_PurchaseOrderRectifications_Create completed successfully ===';
GO
