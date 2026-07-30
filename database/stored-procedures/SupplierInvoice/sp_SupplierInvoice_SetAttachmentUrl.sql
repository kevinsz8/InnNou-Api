SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICE - SET ATTACHMENT URL
   Called once the PDF/scan has been saved to disk by
   ISupplierInvoiceFileStorage — same "generate/save first, persist the
   route after" shape as sp_Order_SetPdfUrl.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoice_SetAttachmentUrl
(
    @SupplierInvoiceToken UNIQUEIDENTIFIER,
    @AttachmentUrl        NVARCHAR(500)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SupplierInvoices
    SET AttachmentUrl = @AttachmentUrl
    WHERE SupplierInvoiceToken = @SupplierInvoiceToken;
END;
GO
