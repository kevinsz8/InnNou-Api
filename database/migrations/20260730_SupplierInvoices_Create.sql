SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: SupplierInvoices (Facturacion Phase B — 3-way matching)

   Registers a supplier invoice against one or more already-RECEIVED
   PurchaseOrders (same Supplier), pre-filled from what was actually
   received, and matches the (possibly buyer-corrected) invoiced
   quantity/net-amount against what was received, within a configurable
   tolerance. See .claude/GoodsReceiptsModule.md's Tax section and
   TODO.md point 5 for the full design — confirmed with the user
   2026-07-30:
     - An invoice can consolidate MULTIPLE PurchaseOrders (same Supplier)
       — Adaco's own "Multiple PO/Invoice" feature is the closest real-world
       precedent, researched before building.
     - A PurchaseOrder is invoiced ENTIRELY in one shot, never partially
       across several invoices — simplest V1, no "remaining to invoice"
       tracking needed. Only RECEIVED (100%) POs are eligible.
     - Matching tolerance (percent AND fixed amount, independently
       evaluated — SAP's "PP tolerance key" pattern) is evaluated on the
       NET (taxable) amount only, never the tax-inclusive total — tax is
       downstream/derived, not something this buyer controls (same
       reasoning SAP/Odoo use: VAT is a recoverable pass-through liability,
       not a cost to reconcile).
     - Out-of-tolerance does NOT block saving — the invoice is stored with
       SupplierInvoiceStatusId = DISCREPANCY, no approval workflow in V1,
       purely a visible/filterable flag for manual review.
     - The attachment (PDF/scan) is stored the same way Order confirmation
       PDFs are — local disk now, authenticated-download-only route (never
       a public static file, since this carries prices) — NOT the
       Supplier-logo pattern, since a logo has no commercial data.

   Idempotent — safe to re-run.
   ============================================================= */

-- New terminal-ish PurchaseOrderStatus — a fully-invoiced PO can never be
-- selected again (enforced by UX_SupplierInvoicePurchaseOrders_PurchaseOrderId
-- below, not by this status alone, but the status keeps it out of every
-- "eligible to invoice" list without a second join).
IF NOT EXISTS (SELECT 1 FROM PurchaseOrderStatuses WHERE Code = 'INVOICED')
    INSERT INTO PurchaseOrderStatuses (Code) VALUES ('INVOICED');
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoiceStatuses')
BEGIN
    CREATE TABLE SupplierInvoiceStatuses
    (
        SupplierInvoiceStatusId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code                    VARCHAR(20)       NOT NULL,
        IsActive                BIT               NOT NULL DEFAULT (1),

        CONSTRAINT UQ_SupplierInvoiceStatuses_Code UNIQUE (Code)
    );
END
GO

-- Seed order matters — the C# SupplierInvoiceStatus enum hardcodes these Ids (Matched=1, Discrepancy=2).
IF NOT EXISTS (SELECT 1 FROM SupplierInvoiceStatuses WHERE Code = 'MATCHED')
    INSERT INTO SupplierInvoiceStatuses (Code) VALUES ('MATCHED');
GO
IF NOT EXISTS (SELECT 1 FROM SupplierInvoiceStatuses WHERE Code = 'DISCREPANCY')
    INSERT INTO SupplierInvoiceStatuses (Code) VALUES ('DISCREPANCY');
GO

-- Nearest-organization-wins hierarchy override, same pattern as
-- EffectiveArticleClassification/sp_ParLevel_GetBelowPar: a Super Asociado sets
-- its own row as the default for the whole tree; an Asociado's own row (if
-- present) takes priority for itself. One row per Organization.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoiceMatchTolerances')
BEGIN
    CREATE TABLE SupplierInvoiceMatchTolerances
    (
        SupplierInvoiceMatchToleranceId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierInvoiceMatchToleranceToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        OrganizationId                     INT               NOT NULL,
        TolerancePercent                   DECIMAL(6,3)      NOT NULL,
        ToleranceAmount                    DECIMAL(18,4)     NOT NULL,
        CreatedUtc                         DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                          VARCHAR(150)      NOT NULL,
        LastUpdatedUtc                     DATETIME2         NULL,
        LastUpdatedBy                      VARCHAR(150)      NULL,

        CONSTRAINT UQ_SupplierInvoiceMatchTolerances_Token UNIQUE (SupplierInvoiceMatchToleranceToken),
        CONSTRAINT UQ_SupplierInvoiceMatchTolerances_OrganizationId UNIQUE (OrganizationId),
        CONSTRAINT FK_SupplierInvoiceMatchTolerances_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT CK_SupplierInvoiceMatchTolerances_TolerancePercent CHECK (TolerancePercent >= 0 AND TolerancePercent <= 100),
        CONSTRAINT CK_SupplierInvoiceMatchTolerances_ToleranceAmount CHECK (ToleranceAmount >= 0)
    );
END
GO

-- Internal sequential numbering for the "Libro Registro de Facturas Recibidas" (Spain's legal
-- requirement to keep an internal registration number for received invoices, distinct from the
-- supplier's own invoice number) — same per-Organization-per-Year atomic-counter shape as
-- PurchaseOrderNumberCounters.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoiceNumberCounters')
BEGIN
    CREATE TABLE SupplierInvoiceNumberCounters
    (
        SupplierInvoiceNumberCounterId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrganizationId                 INT               NOT NULL,
        Year                           INT               NOT NULL,
        LastNumber                     INT               NOT NULL DEFAULT (0),

        CONSTRAINT FK_SupplierInvoiceNumberCounters_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SupplierInvoiceNumberCounters_Org_Year' AND object_id = OBJECT_ID('SupplierInvoiceNumberCounters'))
BEGIN
    CREATE UNIQUE INDEX UX_SupplierInvoiceNumberCounters_Org_Year ON SupplierInvoiceNumberCounters (OrganizationId, Year);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoices')
BEGIN
    CREATE TABLE SupplierInvoices
    (
        SupplierInvoiceId       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierInvoiceToken    UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        OrganizationId          INT               NOT NULL,
        SupplierId              INT               NOT NULL,
        SupplierInvoiceNumber   VARCHAR(100)      NOT NULL, -- the supplier's own invoice number (free text)
        InternalSequentialNumber VARCHAR(20)      NOT NULL, -- FR-{Year}-{5-digit}, Libro Registro number
        InvoiceDate             DATE              NOT NULL,
        SupplierInvoiceStatusId INT               NOT NULL,
        AttachmentUrl           NVARCHAR(500)     NULL, -- authenticated download route, never a static file (carries prices)
        Notes                   NVARCHAR(1000)    NULL,
        CreatedUtc              DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy               VARCHAR(150)      NOT NULL,

        CONSTRAINT UQ_SupplierInvoices_Token UNIQUE (SupplierInvoiceToken),
        CONSTRAINT FK_SupplierInvoices_Organizations FOREIGN KEY (OrganizationId) REFERENCES Organizations (OrganizationId),
        CONSTRAINT FK_SupplierInvoices_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers (SupplierId),
        CONSTRAINT FK_SupplierInvoices_Statuses FOREIGN KEY (SupplierInvoiceStatusId) REFERENCES SupplierInvoiceStatuses (SupplierInvoiceStatusId)
    );

    CREATE UNIQUE INDEX UX_SupplierInvoices_Org_InternalNumber ON SupplierInvoices (OrganizationId, InternalSequentialNumber);
    CREATE INDEX IX_SupplierInvoices_SupplierId ON SupplierInvoices (SupplierId);
    CREATE INDEX IX_SupplierInvoices_OrganizationId ON SupplierInvoices (OrganizationId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoiceLines')
BEGIN
    CREATE TABLE SupplierInvoiceLines
    (
        SupplierInvoiceLineId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierInvoiceLineToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        SupplierInvoiceId        INT               NOT NULL,
        PurchaseOrderLineId      INT               NOT NULL, -- references, never mutates — same append-only shape as GoodsReceiptLine
        ArticleId                INT               NOT NULL, -- denormalized traceability, mirrors GoodsReceiptLine.ArticleId

        QuantityInvoiced         DECIMAL(18,4)     NOT NULL,
        UnitPriceInvoiced        DECIMAL(18,4)     NOT NULL,
        CurrencyCode             VARCHAR(10)       NOT NULL,

        TaxCategoryId            INT               NULL,
        TaxRatePercent           DECIMAL(6,3)      NULL,
        TaxableAmount            DECIMAL(18,4)     NOT NULL, -- QuantityInvoiced * UnitPriceInvoiced (net) — this is what tolerance is evaluated against
        TaxAmount                DECIMAL(18,4)     NULL,
        TotalAmount              DECIMAL(18,4)     NOT NULL, -- TaxableAmount + TaxAmount, informational only

        IsWithinTolerance        BIT               NOT NULL, -- this line's own match result

        CreatedUtc               DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                VARCHAR(150)      NOT NULL,

        CONSTRAINT UQ_SupplierInvoiceLines_Token UNIQUE (SupplierInvoiceLineToken),
        CONSTRAINT FK_SupplierInvoiceLines_Header FOREIGN KEY (SupplierInvoiceId) REFERENCES SupplierInvoices (SupplierInvoiceId),
        CONSTRAINT FK_SupplierInvoiceLines_PurchaseOrderLine FOREIGN KEY (PurchaseOrderLineId) REFERENCES PurchaseOrderLine (PurchaseOrderLineId),
        CONSTRAINT FK_SupplierInvoiceLines_Articles FOREIGN KEY (ArticleId) REFERENCES Articles (ArticleId),
        CONSTRAINT FK_SupplierInvoiceLines_TaxCategories FOREIGN KEY (TaxCategoryId) REFERENCES TaxCategories (TaxCategoryId),
        -- A PurchaseOrderLine can be invoiced at most once ever — matches "a PO is invoiced
        -- entirely, in one shot, never partially across several invoices".
        CONSTRAINT UQ_SupplierInvoiceLines_PurchaseOrderLineId UNIQUE (PurchaseOrderLineId)
    );

    CREATE INDEX IX_SupplierInvoiceLines_SupplierInvoiceId ON SupplierInvoiceLines (SupplierInvoiceId);
END
GO

-- Join table: which PurchaseOrders a SupplierInvoice consolidates. The UNIQUE constraint on
-- PurchaseOrderId alone (not composite) is what enforces "a PO can only ever be invoiced once"
-- at the DB level, not just in the service — same discipline as SupplierReturnLines'
-- GoodsReceiptLineId unique constraint.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoicePurchaseOrders')
BEGIN
    CREATE TABLE SupplierInvoicePurchaseOrders
    (
        SupplierInvoicePurchaseOrderId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierInvoiceId              INT               NOT NULL,
        PurchaseOrderId                INT               NOT NULL,

        CONSTRAINT FK_SupplierInvoicePurchaseOrders_Header FOREIGN KEY (SupplierInvoiceId) REFERENCES SupplierInvoices (SupplierInvoiceId),
        CONSTRAINT FK_SupplierInvoicePurchaseOrders_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrder (PurchaseOrderId),
        CONSTRAINT UQ_SupplierInvoicePurchaseOrders_PurchaseOrderId UNIQUE (PurchaseOrderId)
    );

    CREATE INDEX IX_SupplierInvoicePurchaseOrders_SupplierInvoiceId ON SupplierInvoicePurchaseOrders (SupplierInvoiceId);
END
GO

PRINT '=== Migration 20260730_SupplierInvoices_Create completed successfully ===';
GO
