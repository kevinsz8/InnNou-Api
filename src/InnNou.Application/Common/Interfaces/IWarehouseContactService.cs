using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IWarehouseContactService
    {
        Task<PagedResult<WarehouseContactDto>> GetPagedByWarehouseTokenAsync(Guid warehouseToken, int pageNumber, int pageSize, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<WarehouseContactDto?> GetByTokenAsync(Guid warehouseContactToken, IRequestContext context, CancellationToken cancellationToken);
        Task<WarehouseContactDto?> CreateAsync(WarehouseContactDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<WarehouseContactDto?> EditAsync(WarehouseContactDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid warehouseContactToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
