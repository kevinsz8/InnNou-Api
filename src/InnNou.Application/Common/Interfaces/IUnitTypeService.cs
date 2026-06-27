using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface IUnitTypeService
    {
        Task<List<UnitTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> CreateAsync(UnitTypeDto dto, CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> EditAsync(UnitTypeDto dto, CancellationToken cancellationToken = default);
        Task<UnitTypeDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
