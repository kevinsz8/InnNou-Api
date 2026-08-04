using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISupplierPriceSubscriptionService
    {
        // Full replace, not incremental add/remove — the frontend is a multi-select, not a
        // per-supplier toggle. Only tokens the caller's own organization can actually see (per
        // Supplier global/private scoping) are kept; the rest are silently dropped, never an
        // error, since a stale/unauthorized token in the request isn't the caller's fault.
        Task<List<SupplierPriceChangeSubscriptionDto>> SetSubscriptionsAsync(List<Guid> supplierTokens, IRequestContext context, CancellationToken cancellationToken = default);

        Task<List<SupplierPriceChangeSubscriptionDto>> GetMySubscriptionsAsync(IRequestContext context, CancellationToken cancellationToken = default);
    }
}
