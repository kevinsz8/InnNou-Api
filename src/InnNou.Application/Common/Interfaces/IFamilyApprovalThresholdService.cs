using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IFamilyApprovalThresholdService
    {
        Task<PagedResult<FamilyApprovalThresholdDto>> GetPagedAsync(Guid organizationToken, int pageNumber, int pageSize, Guid? familyToken, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<FamilyApprovalThresholdDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default);
        Task<FamilyApprovalThresholdDto?> CreateAsync(Guid organizationToken, Guid familyToken, int level, decimal thresholdAmount, Guid approverUserToken, IRequestContext context, CancellationToken cancellationToken = default);
        Task<FamilyApprovalThresholdDto?> EditAsync(Guid token, decimal thresholdAmount, Guid approverUserToken, IRequestContext context, CancellationToken cancellationToken = default);
        Task<FamilyApprovalThresholdDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
