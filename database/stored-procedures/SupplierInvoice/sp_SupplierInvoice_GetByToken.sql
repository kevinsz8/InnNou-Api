SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICE - GET BY TOKEN
   Header only — SupplierInvoiceService.GetByTokenAsync hydrates Lines and
   PurchaseOrders via their own dedicated GetBySupplierInvoiceId SPs, same
   "header SP + separate lines SP" shape as GoodsReceipt.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoice_GetByToken
(
    @SupplierInvoiceToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        si.SupplierInvoiceId, si.SupplierInvoiceToken,
        si.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        si.SupplierId, s.SupplierToken, s.Name AS SupplierName,
        si.SupplierInvoiceNumber, si.InternalSequentialNumber, si.InvoiceDate,
        si.SupplierInvoiceStatusId, sis.Code AS Status,
        si.AttachmentUrl, si.Notes, si.CreatedUtc, si.CreatedBy
    FROM dbo.SupplierInvoices si
    JOIN dbo.Organizations org           ON org.OrganizationId = si.OrganizationId
    JOIN dbo.Suppliers s                 ON s.SupplierId       = si.SupplierId
    JOIN dbo.SupplierInvoiceStatuses sis  ON sis.SupplierInvoiceStatusId = si.SupplierInvoiceStatusId
    WHERE si.SupplierInvoiceToken = @SupplierInvoiceToken;
END;
GO
