SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERCREDITNOTEINVOICE - CREATE
   Auto-detected link (never user-picked, see the migration's own header
   comment) — one row per SupplierInvoice found to cover a GoodsReceipt this
   credit note's lines reference. Idempotent per (SupplierCreditNoteId,
   SupplierInvoiceId) — a caller can call this once per resolved invoice
   without pre-deduplicating.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNoteInvoice_Create
(
    @SupplierCreditNoteId INT,
    @SupplierInvoiceId    INT,
    @CreatedBy             VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.SupplierCreditNoteInvoices
        WHERE SupplierCreditNoteId = @SupplierCreditNoteId AND SupplierInvoiceId = @SupplierInvoiceId)
    BEGIN
        INSERT INTO dbo.SupplierCreditNoteInvoices (SupplierCreditNoteId, SupplierInvoiceId, CreatedBy)
        VALUES (@SupplierCreditNoteId, @SupplierInvoiceId, @CreatedBy);
    END;
END;
GO
