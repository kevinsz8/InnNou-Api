using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISubCategoryService
    {
        Task<List<SubCategoryDto>> GetAllAsync(int? categoryId = null, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int categoryId, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> CreateAsync(SubCategoryDto dto, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> EditAsync(SubCategoryDto dto, CancellationToken cancellationToken = default);
        Task<SubCategoryDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
