using InnNou.Application.Common.Interfaces;
using InnNou.Infrastructure.Abstractions;
using Microsoft.Extensions.Configuration;

namespace InnNou.Infrastructure.Services
{
    public class LocalOrderPdfStorage(IConfiguration configuration) : IOrderPdfStorage
    {
        private readonly string _physicalBasePath = OrderPdfPaths.ResolvePhysicalBasePath(configuration);

        public async Task SaveAsync(Guid orderToken, byte[] pdfBytes, CancellationToken cancellationToken)
        {
            var folder = Path.Combine(_physicalBasePath, orderToken.ToString("N"));
            Directory.CreateDirectory(folder);

            var fullPath = Path.Combine(folder, "order.pdf");
            await File.WriteAllBytesAsync(fullPath, pdfBytes, cancellationToken);
        }

        public async Task<byte[]?> GetAsync(Guid orderToken, CancellationToken cancellationToken)
        {
            var fullPath = Path.Combine(_physicalBasePath, orderToken.ToString("N"), "order.pdf");
            return File.Exists(fullPath) ? await File.ReadAllBytesAsync(fullPath, cancellationToken) : null;
        }
    }
}
