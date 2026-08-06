/* =============================================================
   SUPPLIER CREDIT NOTES — Facturación Fase C ("Notas de crédito")
   Date: 2026-08-07
   =============================================================
   Closes TODO.md point 5's Fase C: connects a closed-CREDITED SupplierReturn
   to a real fiscal "factura rectificativa" document. Researched before
   building (RD 1619/2012 arts. 6/15, Ley 37/1992 art. 80, SAP MIRO Credit
   Memo, Odoo vendor credit notes) — see .claude/SupplierCreditNoteModule.md
   for the full design writeup.

   Key facts driving the shape below:
   - A factura rectificativa needs its own separate numbering series (RD
     1619/2012 art. 6, confirmed independently by SAP KB 2519546) — same
     atomic per-Organization-per-Year counter pattern as
     SupplierInvoiceNumberCounters.
   - It must identify the invoice(s) it corrects, but ONLY when one exists
     for the credited quantity — InnNou only ever invoices QuantityAccepted,
     so a return's rejected units frequently were never on any invoice at
     all. SupplierCreditNoteInvoices is populated by AUTO-DETECTING which
     SupplierInvoice(s) (via the existing SupplierInvoiceGoodsReceipts join)
     cover the GoodsReceipts referenced by this credit note's lines — never
     user-picked, and legitimately empty when nothing was ever invoiced.
   - One SupplierReturn can span lines from different GoodsReceipts of the
     same PurchaseOrder, and those receipts can be on DIFFERENT
     SupplierInvoices (invoicing granularity is per-receipt, not per-PO) —
     hence a join table, not a single nullable FK.
   - Exactly one credit note per SupplierReturn (UNIQUE on SupplierReturnId)
     — not repeatable/partial in V1, a deliberate scope decision (a return
     is closed once, credited once).
   - Each SupplierReturnLine can be credited at most once (UNIQUE on
     SupplierCreditNoteLines.SupplierReturnLineId) — same "claimed once"
     shape SupplierReturnLines itself already uses against GoodsReceiptLine.
   - UnitPrice/CurrencyCode/TaxCategoryId/TaxRatePercent default from the
     underlying GoodsReceiptLine's own frozen fields (see
     migrations/20260807_GoodsReceiptLine_AddUnitPrice.sql) but are always
     re-stored on the line itself (never re-read live later) — same
     freeze-and-never-recompute discipline as every other financial
     snapshot in this codebase. WasManuallyEntered flags when the caller had
     to type them because the source GoodsReceiptLine predates that fix (no
     UnitPrice frozen there) — transparency, not a silent guess.
   - SupplierCreditNoteTaxBreakdown is purely COMPUTED (aggregated from the
     lines by TaxRatePercent) — unlike SupplierInvoiceTaxBreakdown, there is
     no separate externally-authored "stated" number to reconcile against
     here (this document originates in InnNou, not a supplier's own PDF), so
     no MATCHED/DISCREPANCY/tolerance machinery applies.

   Idempotent — safe to re-run.
   ============================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.SupplierCreditNoteNumberCounters', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierCreditNoteNumberCounters
    (
        SupplierCreditNoteNumberCounterId INT IDENTITY PRIMARY KEY,
        OrganizationId                    INT NOT NULL REFERENCES dbo.Organizations(OrganizationId),
        Year                              INT NOT NULL,
        LastNumber                        INT NOT NULL,
        CONSTRAINT UQ_SupplierCreditNoteNumberCounters_Org_Year UNIQUE (OrganizationId, Year)
    );
END;
GO

IF OBJECT_ID('dbo.SupplierCreditNotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierCreditNotes
    (
        SupplierCreditNoteId     INT IDENTITY PRIMARY KEY,
        SupplierCreditNoteToken  UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID(),
        SupplierReturnId         INT NOT NULL UNIQUE REFERENCES dbo.SupplierReturns(SupplierReturnId),
        OrganizationId           INT NOT NULL REFERENCES dbo.Organizations(OrganizationId),
        SupplierId               INT NOT NULL REFERENCES dbo.Suppliers(SupplierId),
        CreditNoteNumber         VARCHAR(100)  NOT NULL,
        InternalSequentialNumber VARCHAR(20)   NOT NULL,
        CreditNoteDate           DATE NOT NULL,
        Reason                   NVARCHAR(500) NOT NULL,
        Notes                    NVARCHAR(1000) NULL,
        CreatedUtc               DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                VARCHAR(150) NOT NULL
    );
END;
GO

IF OBJECT_ID('dbo.SupplierCreditNoteLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierCreditNoteLines
    (
        SupplierCreditNoteLineId    INT IDENTITY PRIMARY KEY,
        SupplierCreditNoteLineToken UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID(),
        SupplierCreditNoteId        INT NOT NULL REFERENCES dbo.SupplierCreditNotes(SupplierCreditNoteId),
        SupplierReturnLineId        INT NOT NULL UNIQUE REFERENCES dbo.SupplierReturnLines(SupplierReturnLineId),
        ArticleId                   INT NOT NULL REFERENCES dbo.Articles(ArticleId),
        QuantityCredited            DECIMAL(18,8) NOT NULL,
        UnitPrice                   DECIMAL(18,8) NOT NULL,
        CurrencyCode                VARCHAR(10)   NOT NULL,
        TaxCategoryId               INT NULL REFERENCES dbo.TaxCategories(TaxCategoryId),
        TaxRatePercent              DECIMAL(11,8) NULL,
        TaxableAmount               DECIMAL(18,8) NOT NULL,
        TaxAmount                   DECIMAL(18,8) NOT NULL,
        TotalAmount                 DECIMAL(18,8) NOT NULL,
        WasManuallyEntered          BIT NOT NULL DEFAULT 0,
        CreatedUtc                  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                   VARCHAR(150) NOT NULL
    );
END;
GO

IF OBJECT_ID('dbo.SupplierCreditNoteInvoices', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierCreditNoteInvoices
    (
        SupplierCreditNoteInvoiceId INT IDENTITY PRIMARY KEY,
        SupplierCreditNoteId        INT NOT NULL REFERENCES dbo.SupplierCreditNotes(SupplierCreditNoteId),
        SupplierInvoiceId           INT NOT NULL REFERENCES dbo.SupplierInvoices(SupplierInvoiceId),
        CreatedUtc                  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                   VARCHAR(150) NOT NULL,
        CONSTRAINT UQ_SupplierCreditNoteInvoices_Note_Invoice UNIQUE (SupplierCreditNoteId, SupplierInvoiceId)
    );
END;
GO

IF OBJECT_ID('dbo.SupplierCreditNoteTaxBreakdown', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierCreditNoteTaxBreakdown
    (
        SupplierCreditNoteTaxBreakdownId    INT IDENTITY PRIMARY KEY,
        SupplierCreditNoteTaxBreakdownToken UNIQUEIDENTIFIER NOT NULL UNIQUE DEFAULT NEWID(),
        SupplierCreditNoteId                INT NOT NULL REFERENCES dbo.SupplierCreditNotes(SupplierCreditNoteId),
        TaxRatePercent                       DECIMAL(11,8) NOT NULL,
        TaxableAmount                        DECIMAL(18,8) NOT NULL,
        TaxAmount                            DECIMAL(18,8) NOT NULL,
        CurrencyCode                         VARCHAR(10) NOT NULL,
        CreatedUtc                           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy                            VARCHAR(150) NOT NULL
    );
END;
GO

PRINT '=== Migration 20260807_SupplierCreditNotes_Create completed successfully ===';
GO
