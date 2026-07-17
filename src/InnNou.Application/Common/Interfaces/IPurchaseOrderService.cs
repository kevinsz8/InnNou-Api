using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IPurchaseOrderService
    {
        Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(Guid? organizationToken, Guid? orderToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<PurchaseOrderDto?> GetByTokenAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<PurchaseOrderDto?> CancelAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
