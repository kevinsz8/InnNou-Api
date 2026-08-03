using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface ITaxService
    {
        Task<List<TaxCategoryDto>> GetTaxCategoriesAsync(CancellationToken cancellationToken = default);
        Task<List<TaxJurisdictionDto>> GetTaxJurisdictionsAsync(CancellationToken cancellationToken = default);
        Task<List<TaxRateGridRowDto>> GetTaxRateGridAsync(CancellationToken cancellationToken = default);
        Task<TaxRateGridRowDto?> UpsertTaxRateAsync(Guid taxJurisdictionToken, Guid taxCategoryToken, decimal ratePercent, IRequestContext context, CancellationToken cancellationToken = default);
        Task<TaxJurisdictionDto> CreateTaxJurisdictionAsync(string countryCode, string code, string name, IRequestContext context, CancellationToken cancellationToken = default);
        Task<TaxCategoryDto> CreateTaxCategoryAsync(string code, IRequestContext context, CancellationToken cancellationToken = default);
        Task<List<FamilyTaxCategoryOverrideDto>> GetFamilyTaxCategoryOverridesAsync(Guid familyToken, CancellationToken cancellationToken = default);
        Task<FamilyTaxCategoryOverrideDto> UpsertFamilyTaxCategoryOverrideAsync(Guid familyToken, Guid taxJurisdictionToken, Guid taxCategoryToken, IRequestContext context, CancellationToken cancellationToken = default);
        Task DeleteFamilyTaxCategoryOverrideAsync(Guid familyToken, Guid taxJurisdictionToken, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
