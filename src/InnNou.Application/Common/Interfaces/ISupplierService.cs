using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISupplierService
    {
        Task<PagedResult<SupplierDto>> GetSuppliersAsync(int pageNumber, int pageSize, string? searchField, string? searchText, IRequestContext context, CancellationToken cancellationToken);
        Task<SupplierDto?> GetSupplierByTokenAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken);
        Task<SupplierDto?> CreateSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<SupplierDto?> EditSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteSupplierAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> SupplierExistsAsync(string name, CancellationToken cancellationToken);
    }
}
