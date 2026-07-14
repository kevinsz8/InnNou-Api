using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISubFamilyService
    {
        Task<PagedResult<SubFamilyDto>> GetPagedAsync(int pageNumber, int pageSize, int? familyId = null, string? searchText = null, bool includeInactive = false, CancellationToken cancellationToken = default);
        Task<SubFamilyDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, int familyId, CancellationToken cancellationToken = default);
        Task<SubFamilyDto?> CreateAsync(SubFamilyDto dto, CancellationToken cancellationToken = default);
        Task<SubFamilyDto?> EditAsync(SubFamilyDto dto, CancellationToken cancellationToken = default);
        Task<SubFamilyDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default);
        Task<BulkImportSubFamilyResultDto> BulkImportSubFamiliesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> ExportSubFamiliesAsync(string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> GenerateSubFamilyImportTemplateAsync(IRequestContext context, CancellationToken cancellationToken = default);
    }
}
