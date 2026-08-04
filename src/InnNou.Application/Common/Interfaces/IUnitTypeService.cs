using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IUnitTypeService
    {
        Task<PagedResult<UnitTypeDto>> GetPagedAsync(int pageNumber, int pageSize, bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> CreateAsync(UnitTypeDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> EditAsync(UnitTypeDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> SetNameTranslationsAsync(Guid unitTypeToken, Dictionary<string, string> translations, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
