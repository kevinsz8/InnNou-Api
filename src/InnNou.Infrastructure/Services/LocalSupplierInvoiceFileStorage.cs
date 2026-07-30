using InnNou.Application.Common.Interfaces;
using InnNou.Infrastructure.Abstractions;
using Microsoft.Extensions.Configuration;

namespace InnNou.Infrastructure.Services
{
    public class LocalSupplierInvoiceFileStorage(IConfiguration configuration) : ISupplierInvoiceFileStorage
    {
        private readonly string _physicalBasePath = SupplierInvoiceAttachmentPaths.ResolvePhysicalBasePath(configuration);

        public async Task SaveAsync(Guid supplierInvoiceToken, byte[] fileBytes, string fileExtension, CancellationToken cancellationToken)
        {
            var folder = Path.Combine(_physicalBasePath, supplierInvoiceToken.ToString("N"));
            Directory.CreateDirectory(folder);

            // Remove any previously saved attachment first — a re-upload with a different
            // extension would otherwise leave the old file orphaned on disk.
            foreach (var existingFile in Directory.EnumerateFiles(folder, "invoice.*"))
                File.Delete(existingFile);

            var fullPath = Path.Combine(folder, $"invoice{fileExtension}");
            await File.WriteAllBytesAsync(fullPath, fileBytes, cancellationToken);
        }

        public async Task<(byte[] Bytes, string Extension)?> GetAsync(Guid supplierInvoiceToken, CancellationToken cancellationToken)
        {
            var folder = Path.Combine(_physicalBasePath, supplierInvoiceToken.ToString("N"));
            if (!Directory.Exists(folder))
                return null;

            var file = Directory.EnumerateFiles(folder, "invoice.*").FirstOrDefault();
            if (file is null)
                return null;

            var bytes = await File.ReadAllBytesAsync(file, cancellationToken);
            return (bytes, Path.GetExtension(file));
        }
    }
}
