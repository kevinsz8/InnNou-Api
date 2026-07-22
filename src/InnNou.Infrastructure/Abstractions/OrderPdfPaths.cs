using Microsoft.Extensions.Configuration;

namespace InnNou.Infrastructure.Abstractions
{
    // Resolves the physical folder LocalOrderPdfStorage reads/writes. Unlike SupplierLogoPaths,
    // there is no public URL prefix — the PDF is never served statically (it carries
    // prices/commercial data), only streamed back through the authenticated
    // POST /orders/downloadPdf endpoint.
    public static class OrderPdfPaths
    {
        public static string ResolvePhysicalBasePath(IConfiguration configuration)
        {
            var relative = configuration["FileStorage:OrderPdfsPath"] ?? "UploadedFiles/OrderPdfs";
            return Path.IsPathRooted(relative) ? relative : Path.Combine(Directory.GetCurrentDirectory(), relative);
        }
    }
}
