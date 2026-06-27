using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface IUnitConversionRateService
    {
        Task<List<UnitConversionRateDto>> GetAllAsync(int? unitTypeId = null, CancellationToken cancellationToken = default);
        Task<UnitConversionRateDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<UnitConversionRateDto?> CreateAsync(UnitConversionRateDto dto, CancellationToken cancellationToken = default);
        Task<UnitConversionRateDto?> EditAsync(UnitConversionRateDto dto, CancellationToken cancellationToken = default);
        Task<UnitConversionRateDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
