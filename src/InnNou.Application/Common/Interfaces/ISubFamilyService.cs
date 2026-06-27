using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISubFamilyService
    {
        Task<PagedResult<SubFamilyDto>> GetPagedAsync(int pageNumber, int pageSize, int? familyId = null, string? searchText = null, CancellationToken cancellationToken = default);
        Task<SubFamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int familyId, CancellationToken cancellationToken = default);
        Task<SubFamilyDto?> CreateAsync(SubFamilyDto dto, CancellationToken cancellationToken = default);
        Task<SubFamilyDto?> EditAsync(SubFamilyDto dto, CancellationToken cancellationToken = default);
        Task<SubFamilyDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
