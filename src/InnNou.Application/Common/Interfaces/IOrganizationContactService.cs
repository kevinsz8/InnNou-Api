using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IOrganizationContactService
    {
        Task<PagedResult<OrganizationContactDto>> GetPagedByOrganizationTokenAsync(Guid organizationToken, int pageNumber, int pageSize, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<OrganizationContactDto?> GetByTokenAsync(Guid organizationContactToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrganizationContactDto?> CreateAsync(OrganizationContactDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<OrganizationContactDto?> EditAsync(OrganizationContactDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid organizationContactToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
