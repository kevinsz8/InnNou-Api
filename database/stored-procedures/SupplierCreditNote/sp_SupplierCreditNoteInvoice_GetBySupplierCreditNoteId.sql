SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_SupplierCreditNoteInvoice_GetBySupplierCreditNoteId
(
    @SupplierCreditNoteId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        si.SupplierInvoiceId, si.SupplierInvoiceToken, si.InternalSequentialNumber, si.SupplierInvoiceNumber
    FROM dbo.SupplierCreditNoteInvoices scni
    JOIN dbo.SupplierInvoices si ON si.SupplierInvoiceId = scni.SupplierInvoiceId
    WHERE scni.SupplierCreditNoteId = @SupplierCreditNoteId
    ORDER BY si.SupplierInvoiceId;
END;
GO
