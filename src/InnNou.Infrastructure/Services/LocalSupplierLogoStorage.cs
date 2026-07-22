using InnNou.Application.Common.Interfaces;
using InnNou.Infrastructure.Abstractions;
using Microsoft.Extensions.Configuration;

namespace InnNou.Infrastructure.Services
{
    public class LocalSupplierLogoStorage(IConfiguration configuration) : ISupplierLogoStorage
    {
        private readonly string _physicalBasePath = SupplierLogoPaths.ResolvePhysicalBasePath(configuration);

        public async Task<string> SaveAsync(Guid supplierToken, Stream fileStream, string fileExtension, CancellationToken cancellationToken)
        {
            var folder = Path.Combine(_physicalBasePath, supplierToken.ToString("N"));
            Directory.CreateDirectory(folder);

            // Remove any previously saved logo first — otherwise a re-upload with a different
            // extension would leave the old file orphaned on disk once LogoUrl is overwritten.
            foreach (var existingFile in Directory.EnumerateFiles(folder, "logo.*"))
                File.Delete(existingFile);

            var fileName = $"logo{fileExtension}";
            var fullPath = Path.Combine(folder, fileName);

            await using (var destination = File.Create(fullPath))
            {
                await fileStream.CopyToAsync(destination, cancellationToken);
            }

            return $"{SupplierLogoPaths.PublicUrlPrefix}/{supplierToken:N}/{fileName}";
        }

        public Task DeleteAsync(Guid supplierToken, CancellationToken cancellationToken)
        {
            var folder = Path.Combine(_physicalBasePath, supplierToken.ToString("N"));
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);

            return Task.CompletedTask;
        }
    }
}
