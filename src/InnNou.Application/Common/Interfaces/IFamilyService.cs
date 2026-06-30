using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IFamilyService
    {
        Task<PagedResult<FamilyDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<FamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<FamilyDto?> CreateAsync(FamilyDto dto, CancellationToken cancellationToken = default);
        Task<FamilyDto?> EditAsync(FamilyDto dto, CancellationToken cancellationToken = default);
        Task<FamilyDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
