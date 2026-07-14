using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IOrganizationService
    {
        Task<PagedResult<OrganizationDto>> GetOrganizationsAsync(int pageNumber, int pageSize, string? searchField, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken);
        Task<OrganizationDto?> GetOrganizationByTokenAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> OrganizationExistsByNameAsync(string name, CancellationToken cancellationToken);
        Task<OrganizationDto?> CreateOrganizationAsync(OrganizationDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<OrganizationDto?> EditOrganizationAsync(OrganizationDto dto, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteOrganizationAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken);
        Task<BulkImportOrganizationResultDto> BulkImportOrganizationsAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken);
        Task<(byte[] FileBytes, string FileName)> ExportOrganizationsAsync(string? searchField, string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken);
        Task<(byte[] FileBytes, string FileName)> GenerateOrganizationImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken);
    }
}
