namespace InnNou.Application.Common.Interfaces
{
    // Local-disk attachment storage — expected to be swapped for an S3/Azure Blob-backed
    // implementation later without any caller needing to change, same seam shape as
    // IOrderPdfStorage/ISupplierLogoStorage. Unlike IOrderPdfStorage (always a generated .pdf),
    // an invoice attachment is a user-uploaded scan and can be PDF/JPG/PNG, so the extension
    // travels with the bytes. Never served statically (it carries prices) — only streamed back
    // through the authenticated POST /supplierInvoices/downloadAttachment endpoint.
    public interface ISupplierInvoiceFileStorage
    {
        Task SaveAsync(Guid supplierInvoiceToken, byte[] fileBytes, string fileExtension, CancellationToken cancellationToken);
        Task<(byte[] Bytes, string Extension)?> GetAsync(Guid supplierInvoiceToken, CancellationToken cancellationToken);
    }
}
