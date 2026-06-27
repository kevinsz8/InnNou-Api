using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISubCategoryService
    {
        Task<PagedResult<SubCategoryDto>> GetPagedAsync(int pageNumber, int pageSize, int? categoryId = null, string? searchText = null, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int categoryId, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> CreateAsync(SubCategoryDto dto, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> EditAsync(SubCategoryDto dto, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
