CREATE OR ALTER PROCEDURE sp_SupplierInvoice_HydrateDetails
    @SupplierInvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Combines SupplierInvoiceService.HydrateAsync's 3 independent lookups (Lines, PurchaseOrders,
    -- TaxBreakdown — all keyed only by SupplierInvoiceId) into ONE round trip via multiple result
    -- sets, read through Dapper's QueryMultipleAsync. Each EXEC below forwards that sub-procedure's
    -- own SELECT as-is — none of their logic is duplicated here. See the 2026-08-07 Performance
    -- Optimization backlog, item #5, and sp_Article_ResolveOrderLineDetails for the same technique
    -- applied to OrderService.AddLineAsync.

    -- Result set 1: lines
    EXEC dbo.sp_SupplierInvoiceLine_GetBySupplierInvoiceId @SupplierInvoiceId = @SupplierInvoiceId;

    -- Result set 2: purchase orders covered by this invoice
    EXEC dbo.sp_SupplierInvoicePurchaseOrder_GetBySupplierInvoiceId @SupplierInvoiceId = @SupplierInvoiceId;

    -- Result set 3: per-tax-rate VAT breakdown
    EXEC dbo.sp_SupplierInvoiceTaxBreakdown_GetBySupplierInvoiceId @SupplierInvoiceId = @SupplierInvoiceId;
END;
