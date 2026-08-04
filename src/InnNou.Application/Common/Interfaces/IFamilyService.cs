using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IFamilyService
    {
        Task<PagedResult<FamilyDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<FamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<FamilyDto?> CreateAsync(FamilyDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<FamilyDto?> EditAsync(FamilyDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<FamilyDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<FamilyDto?> SetDefaultTaxCategoryAsync(Guid familyToken, Guid taxCategoryToken, IRequestContext context, CancellationToken cancellationToken = default);

        // Multi-language catalog names, piloted on Family/Category (see .claude/CatalogTranslationsModule.md).
        // Keys are the app's own i18next language codes (en/es/ca) — any subset may be present, resolved
        // client-side with a fallback chain down to Code, same as every other Code/Status field is already
        // translated on the frontend rather than baked into the API response.
        Task<FamilyDto?> SetNameTranslationsAsync(Guid familyToken, Dictionary<string, string> translations, IRequestContext context, CancellationToken cancellationToken = default);
        Task<BulkImportFamilyResultDto> BulkImportFamiliesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> ExportFamiliesAsync(string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> GenerateFamilyImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
