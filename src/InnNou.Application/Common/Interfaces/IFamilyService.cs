using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface IFamilyService
    {
        Task<List<FamilyDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<FamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<FamilyDto?> CreateAsync(FamilyDto dto, CancellationToken cancellationToken = default);
        Task<FamilyDto?> EditAsync(FamilyDto dto, CancellationToken cancellationToken = default);
        Task<FamilyDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
    }
}
