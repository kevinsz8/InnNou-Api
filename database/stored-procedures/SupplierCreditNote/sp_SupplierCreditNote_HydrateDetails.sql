CREATE OR ALTER PROCEDURE sp_SupplierCreditNote_HydrateDetails
    @SupplierCreditNoteId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Combines SupplierCreditNoteService.HydrateAsync's 3 independent lookups (Lines,
    -- TaxBreakdown, CorrectedInvoices — all keyed only by SupplierCreditNoteId) into ONE round
    -- trip via multiple result sets, read through Dapper's QueryMultipleAsync. Each EXEC below
    -- forwards that sub-procedure's own SELECT as-is — none of their logic is duplicated here.
    -- See the 2026-08-07 Performance Optimization backlog, item #5, and
    -- sp_Article_ResolveOrderLineDetails for the same technique applied to OrderService.AddLineAsync.

    -- Result set 1: lines
    EXEC dbo.sp_SupplierCreditNoteLine_GetBySupplierCreditNoteId @SupplierCreditNoteId = @SupplierCreditNoteId;

    -- Result set 2: per-tax-rate VAT breakdown
    EXEC dbo.sp_SupplierCreditNoteTaxBreakdown_GetBySupplierCreditNoteId @SupplierCreditNoteId = @SupplierCreditNoteId;

    -- Result set 3: corrected supplier invoice(s), if any
    EXEC dbo.sp_SupplierCreditNoteInvoice_GetBySupplierCreditNoteId @SupplierCreditNoteId = @SupplierCreditNoteId;
END;
