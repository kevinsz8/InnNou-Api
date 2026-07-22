using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISupplierService
    {
        Task<PagedResult<SupplierDto>> GetSuppliersAsync(int pageNumber, int pageSize, string? searchField, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<SupplierDto?> GetSupplierByTokenAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken);
        Task<SupplierDto?> CreateSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<SupplierDto?> EditSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteSupplierAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken);

        // Logo image lives on local disk (see ISupplierLogoStorage / CLAUDE.md's "Supplier
        // logo" note) — only the resulting relative URL is persisted on the Supplier row.
        // Authorization mirrors EditSupplierAsync's "ordinary field edit" branch exactly.
        Task<SupplierDto?> UploadLogoAsync(Guid supplierToken, Stream fileStream, string fileExtension, IRequestContext context, CancellationToken cancellationToken);
        Task<SupplierDto?> DeleteLogoAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> SupplierExistsAsync(string name, bool isGlobal, int? organizationId, int? excludeSupplierId, CancellationToken cancellationToken);
        Task<BulkImportSupplierResultDto> BulkImportSuppliersAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken);
        Task<(byte[] FileBytes, string FileName)> ExportSuppliersAsync(string? searchField, string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken);
        Task<(byte[] FileBytes, string FileName)> GenerateSupplierImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken);
    }
}
