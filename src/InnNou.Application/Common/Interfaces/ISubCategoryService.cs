using InnNou.Application.Common;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISubCategoryService
    {
        Task<PagedResult<SubCategoryDto>> GetPagedAsync(int pageNumber, int pageSize, int? categoryId, string? searchText, bool includeInactive, IRequestContext context, bool unrestricted = false, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> GetByTokenAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int categoryId, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> CreateAsync(SubCategoryDto dto, IRequestContext context, bool bypassAuthorization = false, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> EditAsync(SubCategoryDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<BulkImportSubCategoryResultDto> BulkImportSubCategoriesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> ExportSubCategoriesAsync(string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> GenerateSubCategoryImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
