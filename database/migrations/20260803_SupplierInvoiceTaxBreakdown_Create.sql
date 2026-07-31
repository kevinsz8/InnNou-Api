SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   MIGRATION: SupplierInvoiceTaxBreakdown (Facturacion Phase B follow-up)

   Captures the VAT/IGIC breakdown (Base Imponible / Tipo / Cuota) exactly
   as it appears on the supplier's real paper/PDF invoice, typed in by the
   user at invoice-creation time — legally required by Spain's Reglamento
   de Facturacion (RD 1619/2012) whenever an invoice spans multiple tax
   rates, and the field a "Libro Registro de Facturas Recibidas" export
   would eventually read from. Additive to SupplierInvoiceLines (which
   already freezes tax per line from the goods receipt) — this is the
   invoice's OWN stated total per rate, an external fact, not something
   derived from our own receipt data. Confirmed with the user 2026-08-03,
   researched against real 3-way-match AP practice first.

   SupplierInvoices.SupplierInvoiceStatusId (MATCHED/DISCREPANCY) is now
   driven by comparing SUM(BaseAmount) here against the expected net total
   from the selected goods receipts, within the org's configured tolerance
   — not by SupplierInvoiceLines.IsWithinTolerance any more, since that
   comparison became structurally always-true once per-line quantity/price
   stopped being editable (the invoiced values are now always exactly what
   was received). IsWithinTolerance stays on SupplierInvoiceLines (still
   computed, harmless) for its own per-line record; it just no longer
   drives the header status.

   Idempotent — safe to re-run.
   ============================================================= */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierInvoiceTaxBreakdown')
BEGIN
    CREATE TABLE SupplierInvoiceTaxBreakdown
    (
        SupplierInvoiceTaxBreakdownId    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierInvoiceTaxBreakdownToken UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
        SupplierInvoiceId                INT               NOT NULL,

        TaxRatePercent                   DECIMAL(6,3)      NULL, -- NULL = untaxed/no-rate bucket
        BaseAmount                       DECIMAL(18,4)     NOT NULL, -- typed by the user, from the real invoice
        TaxAmount                        DECIMAL(18,4)     NOT NULL, -- server-computed: BaseAmount * TaxRatePercent / 100

        CreatedUtc                       DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                        VARCHAR(150)      NOT NULL,

        CONSTRAINT UQ_SupplierInvoiceTaxBreakdown_Token UNIQUE (SupplierInvoiceTaxBreakdownToken),
        CONSTRAINT FK_SupplierInvoiceTaxBreakdown_Header FOREIGN KEY (SupplierInvoiceId) REFERENCES SupplierInvoices (SupplierInvoiceId),
        CONSTRAINT CK_SupplierInvoiceTaxBreakdown_BaseAmount CHECK (BaseAmount >= 0),
        CONSTRAINT CK_SupplierInvoiceTaxBreakdown_TaxAmount CHECK (TaxAmount >= 0)
    );

    CREATE INDEX IX_SupplierInvoiceTaxBreakdown_SupplierInvoiceId ON SupplierInvoiceTaxBreakdown (SupplierInvoiceId);
END
GO

PRINT '=== Migration 20260803_SupplierInvoiceTaxBreakdown_Create completed successfully ===';
GO
