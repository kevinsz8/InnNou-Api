using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IUnitOfMeasureService
    {
        Task<PagedResult<UnitOfMeasureDto>> GetPagedAsync(int pageNumber, int pageSize, int? unitTypeId = null, bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int unitTypeId, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> CreateAsync(UnitOfMeasureDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> EditAsync(UnitOfMeasureDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> SetNameTranslationsAsync(Guid unitOfMeasureToken, Dictionary<string, string> translations, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
