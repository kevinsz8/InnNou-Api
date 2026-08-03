using InnNou.Application.Common;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ICategoryService
    {
        Task<PagedResult<CategoryDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchText, bool includeInactive, IRequestContext context, bool unrestricted = false, CancellationToken cancellationToken = default);
        Task<CategoryDto?> GetByTokenAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int? organizationId, CancellationToken cancellationToken = default);
        Task<CategoryDto?> CreateAsync(CategoryDto dto, IRequestContext context, bool bypassAuthorization = false, CancellationToken cancellationToken = default);
        Task<CategoryDto?> EditAsync(CategoryDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<CategoryDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);

        // Multi-language catalog names, piloted on Family/Category (see .claude/CatalogTranslationsModule.md).
        // Same EnsureCanWriteCategory org-scoped authorization as EditAsync, but bypasses the IsSystem
        // guard sp_Category_Update enforces — translating the display name isn't a Code/identity change.
        Task<CategoryDto?> SetNameTranslationsAsync(Guid categoryToken, Dictionary<string, string> translations, IRequestContext context, CancellationToken cancellationToken = default);
        Task<BulkImportCategoryResultDto> BulkImportCategoriesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> ExportCategoriesAsync(string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> GenerateCategoryImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
