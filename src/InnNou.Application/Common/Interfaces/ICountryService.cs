using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface ICountryService
    {
        Task<List<CountryDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);
    }
}
