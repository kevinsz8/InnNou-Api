using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IDepartmentService
    {
        Task<PagedResult<DepartmentDto>> GetPagedByOrganizationTokenAsync(Guid organizationToken, int pageNumber, int pageSize, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<DepartmentDto?> GetByTokenAsync(Guid departmentToken, IRequestContext context, CancellationToken cancellationToken);
        Task<DepartmentDto?> CreateAsync(DepartmentDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<DepartmentDto?> EditAsync(DepartmentDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<DepartmentDto?> SetActiveAsync(Guid departmentToken, bool isActive, IRequestContext context, CancellationToken cancellationToken);
    }
}
