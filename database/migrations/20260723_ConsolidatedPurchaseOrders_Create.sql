SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: Consolidated Purchase Orders (multi-property spend
   consolidation for negotiation leverage)

   Lets a SUPER_ASSOCIATE organization (the hotel group) pick a Supplier
   + date range and bundle already-existing PurchaseOrders from its
   descendant ASSOCIATE properties into one negotiation snapshot — a
   combined view/PDF for negotiating better pricing, without touching
   PurchaseOrder itself at all. Each property still owns and fulfills
   its own PurchaseOrder exactly as before; this is a pure aggregation
   layer sitting on top, visible only to the Super Asociado (individual
   properties never see it).

   Deliberately no Status/lifecycle — a ConsolidatedPurchaseOrder is an
   immutable snapshot record of a negotiation moment (create once, read
   many, hard-delete if created by mistake), not a stateful workflow
   object like Order/PurchaseOrder.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConsolidatedPurchaseOrders')
BEGIN
    CREATE TABLE ConsolidatedPurchaseOrders
    (
        ConsolidatedPurchaseOrderId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConsolidatedPurchaseOrderToken UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWID(),
        SupplierId                     INT                NOT NULL,
        SuperAssociateOrganizationId   INT                NOT NULL,
        Title                          NVARCHAR(200)      NULL,
        Notes                          NVARCHAR(500)      NULL,
        DateRangeFrom                  DATE               NOT NULL,
        DateRangeTo                    DATE               NOT NULL,
        CreatedUtc                     DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                      VARCHAR(150)       NOT NULL,

        CONSTRAINT FK_ConsolidatedPurchaseOrders_Suppliers
            FOREIGN KEY (SupplierId) REFERENCES Suppliers (SupplierId),
        CONSTRAINT FK_ConsolidatedPurchaseOrders_Organizations
            FOREIGN KEY (SuperAssociateOrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT CK_ConsolidatedPurchaseOrders_DateRange CHECK (DateRangeTo >= DateRangeFrom)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ConsolidatedPurchaseOrders_SuperAssociateOrganizationId' AND object_id = OBJECT_ID('ConsolidatedPurchaseOrders'))
BEGIN
    CREATE INDEX IX_ConsolidatedPurchaseOrders_SuperAssociateOrganizationId ON ConsolidatedPurchaseOrders (SuperAssociateOrganizationId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConsolidatedPurchaseOrderMembers')
BEGIN
    CREATE TABLE ConsolidatedPurchaseOrderMembers
    (
        ConsolidatedPurchaseOrderMemberId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConsolidatedPurchaseOrderId       INT                NOT NULL,
        PurchaseOrderId                   INT                NOT NULL,
        CreatedUtc                        DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                         VARCHAR(150)       NOT NULL,

        CONSTRAINT FK_ConsolidatedPurchaseOrderMembers_Header
            FOREIGN KEY (ConsolidatedPurchaseOrderId) REFERENCES ConsolidatedPurchaseOrders (ConsolidatedPurchaseOrderId) ON DELETE CASCADE,
        CONSTRAINT FK_ConsolidatedPurchaseOrderMembers_PurchaseOrder
            FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrder (PurchaseOrderId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ConsolidatedPurchaseOrderMembers_Header_PO' AND object_id = OBJECT_ID('ConsolidatedPurchaseOrderMembers'))
BEGIN
    CREATE UNIQUE INDEX UX_ConsolidatedPurchaseOrderMembers_Header_PO ON ConsolidatedPurchaseOrderMembers (ConsolidatedPurchaseOrderId, PurchaseOrderId);
END
GO

-- The candidates query (sp_PurchaseOrder_GetCandidatesForConsolidation) excludes any
-- PurchaseOrder already claimed by an existing consolidation, to avoid double-counting
-- negotiated spend across separate snapshots — this index makes that lookup a seek.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ConsolidatedPurchaseOrderMembers_PurchaseOrderId' AND object_id = OBJECT_ID('ConsolidatedPurchaseOrderMembers'))
BEGIN
    CREATE INDEX IX_ConsolidatedPurchaseOrderMembers_PurchaseOrderId ON ConsolidatedPurchaseOrderMembers (PurchaseOrderId);
END
GO

PRINT '=== Migration 20260723_ConsolidatedPurchaseOrders_Create completed successfully ===';
GO
