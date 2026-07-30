using Microsoft.Extensions.Configuration;

namespace InnNou.Infrastructure.Abstractions
{
    // Resolves the physical folder LocalSupplierInvoiceFileStorage reads/writes. No public URL
    // prefix — never served statically (it carries prices), only streamed back through the
    // authenticated POST /supplierInvoices/downloadAttachment endpoint, same as OrderPdfPaths.
    public static class SupplierInvoiceAttachmentPaths
    {
        public static string ResolvePhysicalBasePath(IConfiguration configuration)
        {
            var relative = configuration["FileStorage:SupplierInvoiceAttachmentsPath"] ?? "UploadedFiles/SupplierInvoiceAttachments";
            return Path.IsPathRooted(relative) ? relative : Path.Combine(Directory.GetCurrentDirectory(), relative);
        }
    }
}
