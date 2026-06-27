using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IUnitOfMeasureService
    {
        Task<PagedResult<UnitOfMeasureDto>> GetPagedAsync(int pageNumber, int pageSize, int? unitTypeId = null, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int unitTypeId, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> CreateAsync(UnitOfMeasureDto dto, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> EditAsync(UnitOfMeasureDto dto, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
