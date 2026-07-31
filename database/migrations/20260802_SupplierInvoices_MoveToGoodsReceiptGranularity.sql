SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- MIGRATION: Move Supplier Invoice creation from PurchaseOrder granularity
-- to GoodsReceipt granularity ("Goods-Receipt-Based Invoice Verification",
-- same shape SAP uses for this exact scenario — researched and confirmed
-- with the user before building)
-- Date: 2026-08-02
-- =============================================================
-- A PurchaseOrder can receive multiple partial deliveries over time (each
-- its own GoodsReceipt with its own delivery note/albarán). Real practice:
-- a partial delivery gets invoiced as soon as it arrives — you don't stop
-- operations waiting for the rest, and the remaining delivery becomes a
-- separate invoice later, still against the same PO. The previous model
-- (UQ_SupplierInvoicePurchaseOrders_PurchaseOrderId — a PO could be
-- consolidated into at most ONE invoice, ever) can't express this at all.
--
-- New exclusivity gate: a GoodsReceipt can be invoiced at most once
-- (UQ_SupplierInvoiceGoodsReceipts_GoodsReceiptId), not a PurchaseOrder.
-- SupplierInvoicePurchaseOrders is kept (drives the existing "PEDIDOS DE
-- COMPRA CONSOLIDADOS" chips on the invoice detail page) but its exclusivity
-- constraint is loosened to "at most once per (SupplierInvoiceId,
-- PurchaseOrderId) pair" — the same PO can now legitimately appear across
-- several invoices (one per delivery), just never twice within one invoice.
--
-- SupplierInvoiceLines.GoodsReceiptLineId is nullable — existing rows
-- predate this model and have no reliable GoodsReceiptLine to backfill
-- (same "leave historical rows as-is" precedent as
-- GoodsReceipt.DeliveryNoteNumber's 'N/A' backfill, except here there's no
-- safe placeholder value at all, so NULL is the honest answer). Unlike
-- Postgres, SQL Server's plain UNIQUE constraint allows only ONE null row
-- (not "all nulls are distinct") — so the exclusivity index below is a
-- FILTERED unique index (WHERE GoodsReceiptLineId IS NOT NULL), not a
-- plain UNIQUE constraint, to coexist with the legacy null rows.
--
-- Idempotent — safe to re-run.
-- =============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoiceGoodsReceipts')
BEGIN
    CREATE TABLE SupplierInvoiceGoodsReceipts
    (
        SupplierInvoiceGoodsReceiptId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierInvoiceGoodsReceiptToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        SupplierInvoiceId                INT               NOT NULL,
        GoodsReceiptId                   INT               NOT NULL,
        CreatedUtc                       DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                        VARCHAR(150)      NOT NULL,

        CONSTRAINT UQ_SupplierInvoiceGoodsReceipts_Token UNIQUE (SupplierInvoiceGoodsReceiptToken),
        CONSTRAINT UQ_SupplierInvoiceGoodsReceipts_GoodsReceiptId UNIQUE (GoodsReceiptId),
        CONSTRAINT FK_SupplierInvoiceGoodsReceipts_Header FOREIGN KEY (SupplierInvoiceId) REFERENCES SupplierInvoices (SupplierInvoiceId),
        CONSTRAINT FK_SupplierInvoiceGoodsReceipts_GoodsReceipt FOREIGN KEY (GoodsReceiptId) REFERENCES GoodsReceipt (GoodsReceiptId)
    );

    CREATE INDEX IX_SupplierInvoiceGoodsReceipts_SupplierInvoiceId ON SupplierInvoiceGoodsReceipts (SupplierInvoiceId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SupplierInvoiceLines') AND name = 'GoodsReceiptLineId')
BEGIN
    ALTER TABLE SupplierInvoiceLines ADD GoodsReceiptLineId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SupplierInvoiceLines_GoodsReceiptLine')
BEGIN
    ALTER TABLE SupplierInvoiceLines ADD CONSTRAINT FK_SupplierInvoiceLines_GoodsReceiptLine
        FOREIGN KEY (GoodsReceiptLineId) REFERENCES GoodsReceiptLine (GoodsReceiptLineId);
END
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_SupplierInvoiceLines_PurchaseOrderLineId')
BEGIN
    ALTER TABLE SupplierInvoiceLines DROP CONSTRAINT UQ_SupplierInvoiceLines_PurchaseOrderLineId;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SupplierInvoiceLines_GoodsReceiptLineId' AND object_id = OBJECT_ID('SupplierInvoiceLines'))
BEGIN
    CREATE UNIQUE INDEX UX_SupplierInvoiceLines_GoodsReceiptLineId ON SupplierInvoiceLines (GoodsReceiptLineId) WHERE GoodsReceiptLineId IS NOT NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_SupplierInvoicePurchaseOrders_PurchaseOrderId')
BEGIN
    ALTER TABLE SupplierInvoicePurchaseOrders DROP CONSTRAINT UQ_SupplierInvoicePurchaseOrders_PurchaseOrderId;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_SupplierInvoicePurchaseOrders_Invoice_PurchaseOrder')
BEGIN
    ALTER TABLE SupplierInvoicePurchaseOrders ADD CONSTRAINT UQ_SupplierInvoicePurchaseOrders_Invoice_PurchaseOrder
        UNIQUE (SupplierInvoiceId, PurchaseOrderId);
END
GO

PRINT '=== Migration 20260802_SupplierInvoices_MoveToGoodsReceiptGranularity completed successfully ===';
GO
