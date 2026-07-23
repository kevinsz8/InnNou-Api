using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    // Multi-property spend consolidation — lets a SUPER_ASSOCIATE organization (the hotel
    // group) bundle already-existing, SENT PurchaseOrders from its descendant ASSOCIATE
    // properties (same Supplier, a chosen date range) into one negotiation snapshot: an
    // aggregate-by-article view plus a per-property breakdown, for negotiating better pricing.
    // PurchaseOrder itself is never mutated — this is a pure read-through aggregation layer,
    // visible only to the Super Asociado (individual properties never see it). See
    // .claude/ConsolidatedPurchaseOrderModule.md.
    public interface IConsolidatedPurchaseOrderService
    {
        // superAssociateOrganizationToken is required for a bare SuperAdmin (no organization of
        // their own); a Staff+ caller whose own organization IS the Super Asociado may omit it —
        // it's resolved from context instead. Same shape as CategoryService's write-owner
        // resolution.
        Task<List<PurchaseOrderDto>> GetCandidatesAsync(Guid supplierToken, Guid? superAssociateOrganizationToken, DateTime dateFrom, DateTime dateTo, IRequestContext context, CancellationToken cancellationToken);

        Task<ConsolidatedPurchaseOrderDto?> CreateAsync(Guid supplierToken, Guid? superAssociateOrganizationToken, string? title, string? notes, DateTime dateFrom, DateTime dateTo, List<Guid> purchaseOrderTokens, IRequestContext context, CancellationToken cancellationToken);

        Task<PagedResult<ConsolidatedPurchaseOrderDto>> GetPagedAsync(Guid? superAssociateOrganizationToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);

        Task<ConsolidatedPurchaseOrderDto?> GetByTokenAsync(Guid consolidatedPurchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(Guid consolidatedPurchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);

        // Generated on demand from live data (not persisted to disk) — no email use case exists
        // for this document, unlike Order confirmation.
        Task<(byte[] FileBytes, string FileName)?> GetPdfAsync(Guid consolidatedPurchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
