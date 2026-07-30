using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISupplierService
    {
        // warehouseToken narrows the zone delivery-coverage filter to a specific Warehouse's own
        // Zone (see CLAUDE.md's "Delivery Zones" note) — null means no warehouse in context, so
        // the filter no-ops (e.g. the general admin Suppliers catalog page).
        Task<PagedResult<SupplierDto>> GetSuppliersAsync(int pageNumber, int pageSize, string? searchField, string? searchText, bool includeInactive, Guid? warehouseToken, IRequestContext context, CancellationToken cancellationToken);
        Task<SupplierDto?> GetSupplierByTokenAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken);

        // Aggregates OTD/OTIF/rejection-rate/avg-lead-time over every GoodsReceiptLine received
        // against this supplier within [fromDate, toDate] (both null = all time) — same read
        // visibility as GetSupplierByTokenAsync (if you can see the supplier, you can see its
        // scorecard). Null return means not found or not visible, same as GetSupplierByTokenAsync.
        Task<SupplierScorecardDto?> GetScorecardAsync(Guid supplierToken, DateTime? fromDate, DateTime? toDate, IRequestContext context, CancellationToken cancellationToken);
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
