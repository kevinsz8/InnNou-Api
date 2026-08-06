using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IArticlePriceService
    {
        // notifySubscribers is false only when called from BulkImportArticlePricesAsync's own
        // per-row loop — that method fires one aggregated SUPPLIER_PRICE_UPDATED notification per
        // subscriber after the whole batch finishes instead of one per row, so a large price-list
        // refresh doesn't flood anyone. The single-row endpoint always leaves this true.
        Task<ArticlePriceDto?> CreateAsync(ArticlePriceDto dto, IRequestContext context, CancellationToken cancellationToken = default, bool notifySubscribers = true);
        Task<ArticlePriceDto?> GetCurrentAsync(int articleId, int supplierId, int? requestedOrganizationId, string? currencyCode, DateTime asOfDate, IRequestContext context, CancellationToken cancellationToken = default);
        // Batched sibling of GetCurrentAsync for report-shaped read paths (e.g. price
        // comparison) that need many Articles' current price at once without an N+1 loop.
        // Currency is always resolved from organizationId (never a caller override, unlike
        // GetCurrentAsync) -- an Article with no price in that currency is simply absent from
        // the result, never a partial/blended figure.
        Task<Dictionary<int, (decimal Price, string CurrencyCode)>> GetCurrentBatchAsync(List<int> articleIds, int organizationId, DateTime asOfDate, CancellationToken cancellationToken = default);
        Task<PagedResult<ArticlePriceDto>> GetHistoryAsync(int pageNumber, int pageSize, int articleId, int supplierId, int? requestedOrganizationId, string? currencyCode, IRequestContext context, CancellationToken cancellationToken = default);
        Task<BulkImportArticlePriceResultDto> BulkImportArticlePricesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> ExportArticlePricesAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
