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

        // Goods Receipts ("recepcion de mercaderia") — records what physically arrived against a
        // SENT/PARTIALLY_RECEIVED PurchaseOrder, referencing PurchaseOrderLine without ever
        // mutating it (same append-only shape as Rectifications). Record-only in V1 — no
        // stock/inventory side effects. Each line carries a 3-way quantity split
        // (Accepted/Courtesy/Rejected) — Accepted is capped against the line's
        // remaining-to-receive, Courtesy (supplier FOC/gift surplus) and Rejected
        // (damaged/wrong/short) are uncapped by design. Creating a receipt recomputes the
        // PurchaseOrder's status (SENT -> PARTIALLY_RECEIVED -> RECEIVED) in the same
        // transaction. See .claude/GoodsReceiptsModule.md.
        Task<GoodsReceiptDto?> CreateGoodsReceiptAsync(Guid purchaseOrderToken, string? notes, List<CreateGoodsReceiptLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<GoodsReceiptDto>> GetGoodsReceiptsAsync(Guid? purchaseOrderToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<GoodsReceiptDto?> GetGoodsReceiptByTokenAsync(Guid goodsReceiptToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
