SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: SupplierReturns (devolucion a proveedor / RMA)

   Closes the gap TODO.md's point 2 flagged: GoodsReceiptLine.QuantityRejected/
   RejectionReason already capture that something arrived wrong, but nothing
   downstream ever tracked whether the buyer actually got a credit or
   replacement for it. A SupplierReturn bundles one or more previously-rejected
   GoodsReceiptLines (always from the same PurchaseOrder) into a single case
   the buyer follows until resolved — no real fiscal credit note yet, that's
   Facturación's job (TODO.md point 5); this only tracks the case's own state.

   Status is deliberately just PENDING/CLOSED — a separate ResolutionType
   (CREDITED/REPLACED/WRITTEN_OFF), set only when closing, records *how* it
   was resolved without conflating that with whether the case is still open.

   Built directly Id-backed (lookup tables from the start) per CLAUDE.md's
   "Status/type fields are Id-backed" convention.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierReturnStatuses')
BEGIN
    CREATE TABLE SupplierReturnStatuses
    (
        SupplierReturnStatusId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code                   VARCHAR(20)       NOT NULL,
        IsActive               BIT               NOT NULL DEFAULT (1),

        CONSTRAINT UQ_SupplierReturnStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# SupplierReturnStatus enum hardcodes these Ids (Pending=1, Closed=2).
IF NOT EXISTS (SELECT 1 FROM SupplierReturnStatuses WHERE Code = 'PENDING')
    INSERT INTO SupplierReturnStatuses (Code) VALUES ('PENDING');
GO
IF NOT EXISTS (SELECT 1 FROM SupplierReturnStatuses WHERE Code = 'CLOSED')
    INSERT INTO SupplierReturnStatuses (Code) VALUES ('CLOSED');
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierReturnResolutionTypes')
BEGIN
    CREATE TABLE SupplierReturnResolutionTypes
    (
        SupplierReturnResolutionTypeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code                           VARCHAR(20)       NOT NULL,
        IsActive                       BIT               NOT NULL DEFAULT (1),

        CONSTRAINT UQ_SupplierReturnResolutionTypes_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# SupplierReturnResolutionType enum hardcodes these Ids
-- (Credited=1, Replaced=2, WrittenOff=3).
IF NOT EXISTS (SELECT 1 FROM SupplierReturnResolutionTypes WHERE Code = 'CREDITED')
    INSERT INTO SupplierReturnResolutionTypes (Code) VALUES ('CREDITED');
GO
IF NOT EXISTS (SELECT 1 FROM SupplierReturnResolutionTypes WHERE Code = 'REPLACED')
    INSERT INTO SupplierReturnResolutionTypes (Code) VALUES ('REPLACED');
GO
IF NOT EXISTS (SELECT 1 FROM SupplierReturnResolutionTypes WHERE Code = 'WRITTEN_OFF')
    INSERT INTO SupplierReturnResolutionTypes (Code) VALUES ('WRITTEN_OFF');
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierReturns')
BEGIN
    CREATE TABLE SupplierReturns
    (
        SupplierReturnId               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierReturnToken            UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        PurchaseOrderId                INT               NOT NULL,
        SupplierReturnStatusId         INT               NOT NULL,
        SupplierReturnResolutionTypeId INT               NULL,
        Notes                          NVARCHAR(500)     NULL,
        ClosedUtc                      DATETIME2         NULL,
        ClosedBy                       VARCHAR(150)      NULL,
        CreatedUtc                     DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                      VARCHAR(150)      NOT NULL,

        CONSTRAINT FK_SupplierReturns_PurchaseOrder
            FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrder (PurchaseOrderId),
        CONSTRAINT FK_SupplierReturns_Statuses
            FOREIGN KEY (SupplierReturnStatusId) REFERENCES SupplierReturnStatuses (SupplierReturnStatusId),
        CONSTRAINT FK_SupplierReturns_ResolutionTypes
            FOREIGN KEY (SupplierReturnResolutionTypeId) REFERENCES SupplierReturnResolutionTypes (SupplierReturnResolutionTypeId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierReturns_PurchaseOrderId' AND object_id = OBJECT_ID('SupplierReturns'))
BEGIN
    CREATE INDEX IX_SupplierReturns_PurchaseOrderId ON SupplierReturns (PurchaseOrderId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierReturnLines')
BEGIN
    CREATE TABLE SupplierReturnLines
    (
        SupplierReturnLineId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierReturnLineToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        SupplierReturnId        INT               NOT NULL,
        GoodsReceiptLineId      INT               NOT NULL,
        Notes                   NVARCHAR(500)     NULL,
        CreatedUtc              DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy               VARCHAR(150)      NOT NULL,

        CONSTRAINT FK_SupplierReturnLines_Header
            FOREIGN KEY (SupplierReturnId) REFERENCES SupplierReturns (SupplierReturnId),
        CONSTRAINT FK_SupplierReturnLines_GoodsReceiptLine
            FOREIGN KEY (GoodsReceiptLineId) REFERENCES GoodsReceiptLine (GoodsReceiptLineId),
        -- A rejected GoodsReceiptLine can be claimed by at most one SupplierReturn ever —
        -- the same rejection can't be reclaimed twice into two different cases.
        CONSTRAINT UQ_SupplierReturnLines_GoodsReceiptLineId UNIQUE (GoodsReceiptLineId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierReturnLines_SupplierReturnId' AND object_id = OBJECT_ID('SupplierReturnLines'))
BEGIN
    CREATE INDEX IX_SupplierReturnLines_SupplierReturnId ON SupplierReturnLines (SupplierReturnId);
END
GO

PRINT '=== Migration 20260730_SupplierReturns_Create completed successfully ===';
GO
