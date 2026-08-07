-- =============================================================
-- MIGRATION: Add missing indexes on SupplierCreditNotes and its child tables
-- Date: 2026-08-07
-- =============================================================
-- Found during the full-system audit: 20260807_SupplierCreditNotes_Create.sql
-- never indexed OrganizationId/SupplierId on the header, or the child tables'
-- own SupplierCreditNoteId FK — every one of these is filtered or joined on
-- (sp_SupplierCreditNote_GetPaged, SupplierCreditNoteService.HydrateAsync's
-- three per-read child queries). Mirrors the equivalent indexes already
-- created for SupplierInvoices in 20260730_SupplierInvoices_Create.sql.
--
-- Idempotent — safe to re-run.
-- =============================================================

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierCreditNotes_OrganizationId' AND object_id = OBJECT_ID('dbo.SupplierCreditNotes'))
    CREATE INDEX IX_SupplierCreditNotes_OrganizationId ON dbo.SupplierCreditNotes (OrganizationId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierCreditNotes_SupplierId' AND object_id = OBJECT_ID('dbo.SupplierCreditNotes'))
    CREATE INDEX IX_SupplierCreditNotes_SupplierId ON dbo.SupplierCreditNotes (SupplierId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierCreditNoteLines_SupplierCreditNoteId' AND object_id = OBJECT_ID('dbo.SupplierCreditNoteLines'))
    CREATE INDEX IX_SupplierCreditNoteLines_SupplierCreditNoteId ON dbo.SupplierCreditNoteLines (SupplierCreditNoteId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierCreditNoteInvoices_SupplierCreditNoteId' AND object_id = OBJECT_ID('dbo.SupplierCreditNoteInvoices'))
    CREATE INDEX IX_SupplierCreditNoteInvoices_SupplierCreditNoteId ON dbo.SupplierCreditNoteInvoices (SupplierCreditNoteId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierCreditNoteInvoices_SupplierInvoiceId' AND object_id = OBJECT_ID('dbo.SupplierCreditNoteInvoices'))
    CREATE INDEX IX_SupplierCreditNoteInvoices_SupplierInvoiceId ON dbo.SupplierCreditNoteInvoices (SupplierInvoiceId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SupplierCreditNoteTaxBreakdown_SupplierCreditNoteId' AND object_id = OBJECT_ID('dbo.SupplierCreditNoteTaxBreakdown'))
    CREATE INDEX IX_SupplierCreditNoteTaxBreakdown_SupplierCreditNoteId ON dbo.SupplierCreditNoteTaxBreakdown (SupplierCreditNoteId);
GO

PRINT '=== Migration 20260807_SupplierCreditNotes_AddIndexes completed successfully ===';
GO
