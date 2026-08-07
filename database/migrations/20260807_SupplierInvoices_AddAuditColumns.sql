/* =============================================================
   SUPPLIER INVOICES — add LastUpdatedUtc/LastUpdatedBy audit columns
   Date: 2026-08-07
   =============================================================
   SupplierInvoices was created (20260730_SupplierInvoices_Create.sql) with
   only CreatedUtc/CreatedBy — unlike almost every other entity in this
   codebase, it never got a LastUpdatedUtc/LastUpdatedBy pair. The only write
   an invoice ever receives after creation (the DISCREPANCY status flip in
   SupplierInvoiceService.CreateAsync) therefore left no audit trail of
   who/when. Types match the equivalent columns on SupplierCreditNotes/
   PurchaseOrder-family tables exactly (DATETIME2 NULL / VARCHAR(150) NULL —
   nullable since a freshly-created, never-updated invoice has neither yet).

   Idempotent — safe to re-run.
   ============================================================= */

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.SupplierInvoices', 'LastUpdatedUtc') IS NULL
BEGIN
    ALTER TABLE dbo.SupplierInvoices ADD
        LastUpdatedUtc DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.SupplierInvoices', 'LastUpdatedBy') IS NULL
BEGIN
    ALTER TABLE dbo.SupplierInvoices ADD
        LastUpdatedBy VARCHAR(150) NULL;
END;
GO

PRINT '=== Migration 20260807_SupplierInvoices_AddAuditColumns completed successfully ===';
GO
