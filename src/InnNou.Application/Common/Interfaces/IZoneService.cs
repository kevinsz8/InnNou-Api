using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IZoneService
    {
        Task<PagedResult<ZoneDto>> GetPagedAsync(int pageNumber, int pageSize, string? countryCode, string? searchText, bool includeInactive, CancellationToken cancellationToken = default);
        Task<ZoneDto?> GetByTokenAsync(Guid zoneToken, CancellationToken cancellationToken = default);
        Task<ZoneDto?> CreateAsync(ZoneDto dto, string countryCode, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ZoneDto?> EditAsync(ZoneDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ZoneDto?> SetActiveAsync(Guid zoneToken, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
