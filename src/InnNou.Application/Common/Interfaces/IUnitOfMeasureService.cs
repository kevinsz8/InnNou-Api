using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface IUnitOfMeasureService
    {
        Task<List<UnitOfMeasureDto>> GetAllAsync(int? unitTypeId = null, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int unitTypeId, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> CreateAsync(UnitOfMeasureDto dto, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> EditAsync(UnitOfMeasureDto dto, CancellationToken cancellationToken = default);
        Task<UnitOfMeasureDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
