SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICE - SET STATUS
   Sets SupplierInvoiceStatusId (MATCHED/DISCREPANCY) by token, stamping
   LastUpdatedUtc/LastUpdatedBy (see
   migrations/20260807_SupplierInvoices_AddAuditColumns.sql) — replaces the
   raw parameterized UPDATE string SupplierInvoiceService.CreateAsync used to
   call directly, the only write in the Supplier Invoice/Credit Note pair
   that skipped the "everything goes through a stored procedure" rule. Same
   shape as sp_PurchaseOrder_SetStatus.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoice_SetStatus
(
    @SupplierInvoiceToken   UNIQUEIDENTIFIER,
    @SupplierInvoiceStatusId INT,
    @LastUpdatedUtc         DATETIME2,
    @LastUpdatedBy          VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SupplierInvoices
    SET SupplierInvoiceStatusId = @SupplierInvoiceStatusId,
        LastUpdatedUtc          = @LastUpdatedUtc,
        LastUpdatedBy           = @LastUpdatedBy
    WHERE SupplierInvoiceToken = @SupplierInvoiceToken;
END;
GO
