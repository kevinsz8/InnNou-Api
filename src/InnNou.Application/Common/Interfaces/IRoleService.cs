using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IRoleService
    {
        Task<PagedResult<RoleDto>> GetRolesAsync(int pageNumber, int pageSize, string? searchField, string? searchText, IRequestContext context, CancellationToken cancellationToken);
    }
}
