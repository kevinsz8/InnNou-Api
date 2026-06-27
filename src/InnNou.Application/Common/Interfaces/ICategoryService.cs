using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<CategoryDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<CategoryDto?> CreateAsync(CategoryDto dto, CancellationToken cancellationToken = default);
        Task<CategoryDto?> EditAsync(CategoryDto dto, CancellationToken cancellationToken = default);
        Task<CategoryDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
