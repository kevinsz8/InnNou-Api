using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface ITaxService
    {
        Task<List<TaxCategoryDto>> GetTaxCategoriesAsync(CancellationToken cancellationToken = default);
        Task<List<TaxJurisdictionDto>> GetTaxJurisdictionsAsync(CancellationToken cancellationToken = default);
        Task<List<TaxRateGridRowDto>> GetTaxRateGridAsync(CancellationToken cancellationToken = default);
        Task<TaxRateGridRowDto?> UpsertTaxRateAsync(Guid taxJurisdictionToken, Guid taxCategoryToken, decimal ratePercent, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
