using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ICategoryService
    {
        Task<PagedResult<CategoryDto>> GetPagedAsync(int pageNumber, int pageSize, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<CategoryDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<CategoryDto?> CreateAsync(CategoryDto dto, CancellationToken cancellationToken = default);
        Task<CategoryDto?> EditAsync(CategoryDto dto, CancellationToken cancellationToken = default);
        Task<CategoryDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
        Task<BulkImportCategoryResultDto> BulkImportCategoriesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> ExportCategoriesAsync(string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> GenerateCategoryImportTemplateAsync(IRequestContext context, CancellationToken cancellationToken = default);
    }
}
