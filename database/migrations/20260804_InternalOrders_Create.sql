-- =============================================================
-- MIGRATION: Create Internal Orders ("Pedidos Internos") module
-- Date: 2026-08-04
-- =============================================================
-- Distinct from Inventory Transfers ("Traspasos", InventoryTransfers/
-- InventoryTransferLines, same-Organization only, no PO, no invoice): an
-- Internal Order moves stock BETWEEN two different Asociado Organizations
-- (each its own legal entity/NIF, even under the same Super Asociado).
-- Researched before building (SAP Stock Transport Order, Dynamics 365
-- intercompany PO, Odoo inter-company PO/SO pairs) — the consistent pattern
-- across all three: same legal entity = a plain stock transfer, different
-- legal entities = a real PO-shaped document that DOES get invoiced (real
-- AR/AP settlement between separate taxable persons, even within the same
-- corporate group). Confirmed with the user this applies here: Spanish IVA
-- law taxes per legal entity (sujeto pasivo), not per economic group — a
-- delivery between two different NIFs is subject to IVA by default, REGE
-- (Régimen Especial de Grupo de Entidades) is an explicit election that
-- doesn't even eliminate IVA on intra-group operations, just changes the
-- taxable base. So an Internal Order's receipt freezes real tax the same
-- way GoodsReceiptLine already does.
--
-- V1 design decisions confirmed with the user:
-- - Deliberately a separate domain (own tables, own service) rather than
--   folding into Order/PurchaseOrder/SupplierInvoice or modeling the source
--   Organization as a special Supplier — keeps this free to grow its own
--   logic later without touching the real external-purchasing pipeline.
-- - Price: the destination Organization's own resolved ArticlePrice (no new
--   "transfer price" concept — also happens to satisfy Art. 18 LIS's
--   related-party market-price requirement for free).
-- - Scope: any Asociado may internal-order from any other Asociado under
--   the same Super Asociado — no separate configurable relationship table
--   for V1 (unlike Delivery Zones' explicit Supplier<->Zone coverage).
-- - No FamilyApprovalThreshold gate in V1 (unlike a real Order/Submit).
-- - Shipping and receiving are each their own append-only, REPEATABLE
--   documents (InternalOrderShipment(s) / InternalOrderReceipt(s)) rather
--   than a single one-shot status flip — researched: both SAP STOs
--   ("Transfer Order for Multiple Outbound Deliveries") and Odoo
--   (automatic backorders) support multiple partial shipments over time
--   against one order, so InternalOrderLine is never mutated after
--   creation, same "freeze once, append the rest" shape as
--   PurchaseOrderLine -> GoodsReceiptLine.
-- - Receiving is a 2-way Accepted/Rejected split, not GoodsReceipt's 3-way
--   Accepted/Courtesy/Rejected — researched: no system found models a
--   "courtesy free surplus" concept for an internal/intercompany transfer,
--   that's specific to an external supplier relationship.
--
-- Idempotent — safe to re-run.
-- =============================================================

-- ── InternalOrderStatuses (lookup) ──────────────────────────────────────
IF OBJECT_ID('InternalOrderStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE InternalOrderStatuses (
        InternalOrderStatusId int         NOT NULL IDENTITY(1,1),
        Code                  varchar(20) NOT NULL,
        IsActive              bit         NOT NULL DEFAULT 1,

        CONSTRAINT PK_InternalOrderStatuses PRIMARY KEY (InternalOrderStatusId),
        CONSTRAINT UQ_InternalOrderStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# InternalOrderStatus enum hardcodes these Ids.
IF NOT EXISTS (SELECT 1 FROM InternalOrderStatuses WHERE Code = 'REQUESTED')
    INSERT INTO InternalOrderStatuses (Code) VALUES ('REQUESTED');
GO
IF NOT EXISTS (SELECT 1 FROM InternalOrderStatuses WHERE Code = 'SHIPPED')
    INSERT INTO InternalOrderStatuses (Code) VALUES ('SHIPPED');
GO
IF NOT EXISTS (SELECT 1 FROM InternalOrderStatuses WHERE Code = 'PARTIALLY_RECEIVED')
    INSERT INTO InternalOrderStatuses (Code) VALUES ('PARTIALLY_RECEIVED');
GO
IF NOT EXISTS (SELECT 1 FROM InternalOrderStatuses WHERE Code = 'RECEIVED')
    INSERT INTO InternalOrderStatuses (Code) VALUES ('RECEIVED');
GO
IF NOT EXISTS (SELECT 1 FROM InternalOrderStatuses WHERE Code = 'CANCELLED')
    INSERT INTO InternalOrderStatuses (Code) VALUES ('CANCELLED');
GO

-- ── InternalOrderNumberCounters (sequential numbering, same recipe as PurchaseOrderNumberCounters) ──
IF OBJECT_ID('InternalOrderNumberCounters', 'U') IS NULL
BEGIN
    CREATE TABLE InternalOrderNumberCounters (
        InternalOrderNumberCounterId int NOT NULL IDENTITY(1,1),
        OrganizationId               int NOT NULL,   -- the REQUESTING Organization's own sequence
        Year                         int NOT NULL,
        LastNumber                   int NOT NULL DEFAULT (0),

        CONSTRAINT PK_InternalOrderNumberCounters PRIMARY KEY (InternalOrderNumberCounterId),
        CONSTRAINT FK_InternalOrderNumberCounters_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
    );

    CREATE UNIQUE INDEX UX_InternalOrderNumberCounters_Org_Year ON InternalOrderNumberCounters (OrganizationId, Year);
END
GO

-- ── InternalOrders (header) ─────────────────────────────────────────────
IF OBJECT_ID('InternalOrders', 'U') IS NULL
BEGIN
    CREATE TABLE InternalOrders (
        InternalOrderId           int              NOT NULL IDENTITY(1,1),
        InternalOrderToken        uniqueidentifier NOT NULL DEFAULT NEWID(),
        InternalOrderNumber       varchar(20)      NOT NULL,   -- PI-{Year}-{5-digit}, e.g. PI-2026-00042

        RequestingOrganizationId  int              NOT NULL,   -- the Asociado asking for stock
        SourceOrganizationId      int              NOT NULL,   -- the Asociado expected to supply it
        DestinationWarehouseId    int              NOT NULL,   -- belongs to RequestingOrganizationId

        InternalOrderStatusId     int              NOT NULL DEFAULT (1),  -- 1 = REQUESTED

        Notes                     nvarchar(1000)       NULL,

        CancelledUtc              datetime2            NULL,
        CancelledBy               varchar(150)         NULL,
        CancelledReason           nvarchar(500)        NULL,

        CreatedUtc                datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                 varchar(150)     NOT NULL,
        LastUpdatedUtc            datetime2            NULL,
        LastUpdatedBy             varchar(150)         NULL,

        CONSTRAINT PK_InternalOrders PRIMARY KEY (InternalOrderId),
        CONSTRAINT FK_InternalOrders_RequestingOrganization FOREIGN KEY (RequestingOrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_InternalOrders_SourceOrganization FOREIGN KEY (SourceOrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_InternalOrders_DestinationWarehouse FOREIGN KEY (DestinationWarehouseId) REFERENCES Warehouses (WarehouseId),
        CONSTRAINT FK_InternalOrders_InternalOrderStatuses FOREIGN KEY (InternalOrderStatusId) REFERENCES InternalOrderStatuses (InternalOrderStatusId),
        CONSTRAINT CK_InternalOrders_DifferentOrganizations CHECK (RequestingOrganizationId <> SourceOrganizationId)
    );

    CREATE UNIQUE INDEX UQ_InternalOrders_InternalOrderToken ON InternalOrders (InternalOrderToken);
    CREATE UNIQUE INDEX UX_InternalOrders_RequestingOrg_Number ON InternalOrders (RequestingOrganizationId, InternalOrderNumber);
    CREATE INDEX IX_InternalOrders_SourceOrganizationId ON InternalOrders (SourceOrganizationId);
    CREATE INDEX IX_InternalOrders_DestinationWarehouseId ON InternalOrders (DestinationWarehouseId);
    CREATE INDEX IX_InternalOrders_InternalOrderStatusId ON InternalOrders (InternalOrderStatusId);
END
GO

-- ── InternalOrderLines — the firm request, immutable once created (never mutated after, same
--    convention as PurchaseOrderLine) ────────────────────────────────────
IF OBJECT_ID('InternalOrderLines', 'U') IS NULL
BEGIN
    CREATE TABLE InternalOrderLines (
        InternalOrderLineId      int              NOT NULL IDENTITY(1,1),
        InternalOrderLineToken   uniqueidentifier NOT NULL DEFAULT NEWID(),
        InternalOrderId          int              NOT NULL,
        ArticleId                int              NOT NULL,

        Quantity                 decimal(18,8)    NOT NULL,   -- requested quantity, PurchaseUnitId-denominated
        UnitPrice                decimal(18,8)    NOT NULL,   -- destination Organization's resolved ArticlePrice, frozen
        CurrencyCode             varchar(3)       NOT NULL,

        Notes                    nvarchar(500)        NULL,

        CreatedUtc               datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                varchar(150)     NOT NULL,

        CONSTRAINT PK_InternalOrderLines PRIMARY KEY (InternalOrderLineId),
        CONSTRAINT FK_InternalOrderLines_InternalOrders FOREIGN KEY (InternalOrderId) REFERENCES InternalOrders (InternalOrderId),
        CONSTRAINT FK_InternalOrderLines_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT CK_InternalOrderLines_QuantityPositive CHECK (Quantity > 0)
    );

    CREATE UNIQUE INDEX UQ_InternalOrderLines_InternalOrderLineToken ON InternalOrderLines (InternalOrderLineToken);
    CREATE INDEX IX_InternalOrderLines_InternalOrderId ON InternalOrderLines (InternalOrderId);
END
GO

-- ── InternalOrderShipments (header) — the source Organization's dispatch event(s). Repeatable:
--    an InternalOrder can be shipped in more than one batch over time (stock-shortage-today,
--    ship-the-rest-later — same reason SAP STOs/Odoo backorders allow it). ──
IF OBJECT_ID('InternalOrderShipments', 'U') IS NULL
BEGIN
    CREATE TABLE InternalOrderShipments (
        InternalOrderShipmentId    int              NOT NULL IDENTITY(1,1),
        InternalOrderShipmentToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        InternalOrderId            int              NOT NULL,
        SourceWarehouseId          int              NOT NULL,   -- belongs to InternalOrders.SourceOrganizationId

        Notes                      nvarchar(1000)       NULL,

        CreatedUtc                 datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                  varchar(150)     NOT NULL,

        CONSTRAINT PK_InternalOrderShipments PRIMARY KEY (InternalOrderShipmentId),
        CONSTRAINT FK_InternalOrderShipments_InternalOrders FOREIGN KEY (InternalOrderId) REFERENCES InternalOrders (InternalOrderId),
        CONSTRAINT FK_InternalOrderShipments_SourceWarehouse FOREIGN KEY (SourceWarehouseId) REFERENCES Warehouses (WarehouseId)
    );

    CREATE UNIQUE INDEX UQ_InternalOrderShipments_InternalOrderShipmentToken ON InternalOrderShipments (InternalOrderShipmentToken);
    CREATE INDEX IX_InternalOrderShipments_InternalOrderId ON InternalOrderShipments (InternalOrderId);
END
GO

IF OBJECT_ID('InternalOrderShipmentLines', 'U') IS NULL
BEGIN
    CREATE TABLE InternalOrderShipmentLines (
        InternalOrderShipmentLineId    int              NOT NULL IDENTITY(1,1),
        InternalOrderShipmentLineToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        InternalOrderShipmentId        int              NOT NULL,
        InternalOrderLineId             int              NOT NULL,

        QuantityShipped                 decimal(18,8)    NOT NULL,

        Notes                            nvarchar(500)        NULL,

        CreatedUtc                       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                        varchar(150)     NOT NULL,

        CONSTRAINT PK_InternalOrderShipmentLines PRIMARY KEY (InternalOrderShipmentLineId),
        CONSTRAINT FK_InternalOrderShipmentLines_InternalOrderShipments FOREIGN KEY (InternalOrderShipmentId) REFERENCES InternalOrderShipments (InternalOrderShipmentId),
        CONSTRAINT FK_InternalOrderShipmentLines_InternalOrderLines FOREIGN KEY (InternalOrderLineId) REFERENCES InternalOrderLines (InternalOrderLineId),
        CONSTRAINT CK_InternalOrderShipmentLines_QuantityPositive CHECK (QuantityShipped > 0)
    );

    CREATE UNIQUE INDEX UQ_InternalOrderShipmentLines_Token ON InternalOrderShipmentLines (InternalOrderShipmentLineToken);
    CREATE INDEX IX_InternalOrderShipmentLines_ShipmentId ON InternalOrderShipmentLines (InternalOrderShipmentId);
    CREATE INDEX IX_InternalOrderShipmentLines_LineId ON InternalOrderShipmentLines (InternalOrderLineId);
END
GO

-- ── InternalOrderReceipts (header) — the destination Organization's receipt event(s), same
--    repeatable shape as GoodsReceipt. ──────────────────────────────────
IF OBJECT_ID('InternalOrderReceipts', 'U') IS NULL
BEGIN
    CREATE TABLE InternalOrderReceipts (
        InternalOrderReceiptId    int              NOT NULL IDENTITY(1,1),
        InternalOrderReceiptToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        InternalOrderId           int              NOT NULL,

        Notes                     nvarchar(1000)       NULL,

        CreatedUtc                datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                 varchar(150)     NOT NULL,

        CONSTRAINT PK_InternalOrderReceipts PRIMARY KEY (InternalOrderReceiptId),
        CONSTRAINT FK_InternalOrderReceipts_InternalOrders FOREIGN KEY (InternalOrderId) REFERENCES InternalOrders (InternalOrderId)
    );

    CREATE UNIQUE INDEX UQ_InternalOrderReceipts_Token ON InternalOrderReceipts (InternalOrderReceiptToken);
    CREATE INDEX IX_InternalOrderReceipts_InternalOrderId ON InternalOrderReceipts (InternalOrderId);
END
GO

-- ── InternalOrderReceiptLines — 2-way Accepted/Rejected split (no Courtesy, see header note),
--    tax snapshot frozen here exactly like GoodsReceiptLine's own. Each line closes out against
--    a specific InternalOrderShipmentLine (what was actually dispatched), not the original
--    InternalOrderLine directly — mirrors GoodsReceiptLine -> PurchaseOrderLine. ──
IF OBJECT_ID('InternalOrderReceiptLines', 'U') IS NULL
BEGIN
    CREATE TABLE InternalOrderReceiptLines (
        InternalOrderReceiptLineId    int              NOT NULL IDENTITY(1,1),
        InternalOrderReceiptLineToken uniqueidentifier NOT NULL DEFAULT NEWID(),
        InternalOrderReceiptId        int              NOT NULL,
        InternalOrderShipmentLineId   int              NOT NULL,

        QuantityAccepted              decimal(18,8)    NOT NULL DEFAULT (0),
        QuantityRejected              decimal(18,8)    NOT NULL DEFAULT (0),
        RejectionReason                nvarchar(500)        NULL,

        TaxCategoryId                  int                  NULL,
        TaxRateId                      int                  NULL,
        TaxRatePercent                 decimal(11,8)        NULL,
        TaxableAmount                  decimal(18,8)        NULL,
        TaxAmount                      decimal(18,8)        NULL,
        TotalAmount                    decimal(18,8)        NULL,

        Notes                           nvarchar(500)        NULL,

        CreatedUtc                      datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                       varchar(150)     NOT NULL,

        CONSTRAINT PK_InternalOrderReceiptLines PRIMARY KEY (InternalOrderReceiptLineId),
        CONSTRAINT FK_InternalOrderReceiptLines_InternalOrderReceipts FOREIGN KEY (InternalOrderReceiptId) REFERENCES InternalOrderReceipts (InternalOrderReceiptId),
        CONSTRAINT FK_InternalOrderReceiptLines_ShipmentLines FOREIGN KEY (InternalOrderShipmentLineId) REFERENCES InternalOrderShipmentLines (InternalOrderShipmentLineId),
        CONSTRAINT FK_InternalOrderReceiptLines_TaxCategories FOREIGN KEY (TaxCategoryId) REFERENCES TaxCategories (TaxCategoryId),
        CONSTRAINT FK_InternalOrderReceiptLines_TaxRates FOREIGN KEY (TaxRateId) REFERENCES TaxRates (TaxRateId),
        CONSTRAINT CK_InternalOrderReceiptLines_QuantitiesNonNegative CHECK (QuantityAccepted >= 0 AND QuantityRejected >= 0),
        CONSTRAINT CK_InternalOrderReceiptLines_AtLeastOne CHECK (QuantityAccepted + QuantityRejected > 0),
        CONSTRAINT CK_InternalOrderReceiptLines_RejectionReasonRequired CHECK (QuantityRejected = 0 OR RejectionReason IS NOT NULL)
    );

    CREATE UNIQUE INDEX UQ_InternalOrderReceiptLines_Token ON InternalOrderReceiptLines (InternalOrderReceiptLineToken);
    CREATE INDEX IX_InternalOrderReceiptLines_ReceiptId ON InternalOrderReceiptLines (InternalOrderReceiptId);
    CREATE INDEX IX_InternalOrderReceiptLines_ShipmentLineId ON InternalOrderReceiptLines (InternalOrderShipmentLineId);
END
GO

-- ── InventoryMovementTypes: 2 new Codes for this module's own stock effect (kept distinct from
--    RECEIPT/TRANSFER_OUT/TRANSFER_IN so the audit trail always shows the real cause). ──
IF NOT EXISTS (SELECT 1 FROM InventoryMovementTypes WHERE Code = 'INTERNAL_ORDER_OUT')
    INSERT INTO InventoryMovementTypes (Code) VALUES ('INTERNAL_ORDER_OUT');
GO
IF NOT EXISTS (SELECT 1 FROM InventoryMovementTypes WHERE Code = 'INTERNAL_ORDER_IN')
    INSERT INTO InventoryMovementTypes (Code) VALUES ('INTERNAL_ORDER_IN');
GO

-- InventoryMovements gains 2 more nullable "origin" FK columns, same shape as its existing
-- GoodsReceiptLineId/InventoryTransferLineId pair — exactly one of the (now four) origin columns
-- is ever set per row, depending on InventoryMovementTypeId.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InventoryMovements') AND name = 'InternalOrderShipmentLineId')
BEGIN
    ALTER TABLE InventoryMovements ADD
        InternalOrderShipmentLineId int NULL,
        InternalOrderReceiptLineId  int NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_InventoryMovements_InternalOrderShipmentLines')
    ALTER TABLE InventoryMovements ADD CONSTRAINT FK_InventoryMovements_InternalOrderShipmentLines FOREIGN KEY (InternalOrderShipmentLineId) REFERENCES InternalOrderShipmentLines (InternalOrderShipmentLineId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_InventoryMovements_InternalOrderReceiptLines')
    ALTER TABLE InventoryMovements ADD CONSTRAINT FK_InventoryMovements_InternalOrderReceiptLines FOREIGN KEY (InternalOrderReceiptLineId) REFERENCES InternalOrderReceiptLines (InternalOrderReceiptLineId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryMovements_InternalOrderShipmentLineId' AND object_id = OBJECT_ID('InventoryMovements'))
    CREATE INDEX IX_InventoryMovements_InternalOrderShipmentLineId ON InventoryMovements (InternalOrderShipmentLineId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryMovements_InternalOrderReceiptLineId' AND object_id = OBJECT_ID('InventoryMovements'))
    CREATE INDEX IX_InventoryMovements_InternalOrderReceiptLineId ON InventoryMovements (InternalOrderReceiptLineId);
GO

PRINT '=== Migration 20260804_InternalOrders_Create completed successfully ===';
GO
