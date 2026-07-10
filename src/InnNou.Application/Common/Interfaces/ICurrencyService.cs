using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface ICurrencyService
    {
        Task<List<CurrencyDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);
        Task<bool> ExistsActiveByCodeAsync(string code, CancellationToken cancellationToken = default);
    }
}
