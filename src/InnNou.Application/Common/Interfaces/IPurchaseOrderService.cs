using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IPurchaseOrderService
    {
        Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(Guid? organizationToken, Guid? orderToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<PurchaseOrderDto?> GetByTokenAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<PurchaseOrderDto?> CancelAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);

        // "Rectificacion de pedido" — post-send corrections to a SENT PurchaseOrder's lines
        // (quantity/price change or full line cancellation), distinct from Goods Receipts (what
        // physically arrived) and from a fiscal Factura Rectificativa. Append-only: a
        // PurchaseOrderLine is never mutated, only ever superseded for display purposes by the
        // latest APPLIED rectification (see sp_PurchaseOrderLine_GetEffective). A rectification
        // that pushes a Family's total (across the WHOLE originating Order, all sibling
        // PurchaseOrders) past a not-yet-approved threshold level is held at PENDING_APPROVAL and
        // reuses the existing OrderApprovalStep machinery; otherwise it applies immediately. See
        // .claude/PurchaseOrderRectificationModule.md.
        Task<PurchaseOrderRectificationDto?> CreateRectificationAsync(Guid purchaseOrderToken, string reason, string? notes, List<RectifyPurchaseOrderLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken);
        Task<List<PurchaseOrderRectificationDto>> GetRectificationsAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<PurchaseOrderRectificationDto?> GetRectificationByTokenAsync(Guid rectificationToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
