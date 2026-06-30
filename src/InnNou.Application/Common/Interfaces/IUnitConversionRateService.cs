using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IUnitConversionRateService
    {
        Task<PagedResult<UnitConversionRateDto>> GetPagedAsync(int pageNumber, int pageSize, int? unitTypeId = null, bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<UnitConversionRateDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<UnitConversionRateDto?> CreateAsync(UnitConversionRateDto dto, CancellationToken cancellationToken = default);
        Task<UnitConversionRateDto?> EditAsync(UnitConversionRateDto dto, CancellationToken cancellationToken = default);
        Task<UnitConversionRateDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid token, CancellationToken cancellationToken = default);
    }
}
