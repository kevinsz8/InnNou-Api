using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IWarehouseService
    {
        Task<PagedResult<WarehouseDto>> GetPagedByOrganizationTokenAsync(Guid organizationToken, int pageNumber, int pageSize, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<WarehouseDto?> GetByTokenAsync(Guid warehouseToken, IRequestContext context, CancellationToken cancellationToken);
        Task<WarehouseDto?> CreateAsync(WarehouseDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<WarehouseDto?> EditAsync(WarehouseDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<WarehouseDto?> SetActiveAsync(Guid warehouseToken, bool isActive, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid warehouseToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
