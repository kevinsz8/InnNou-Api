using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IOrderService
    {
        Task<PagedResult<OrderDto>> GetPagedAsync(Guid? warehouseToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> GetByTokenAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);

        // Streams back the order-confirmation PDF generated best-effort at confirmation time
        // (see OrderService.CompleteSubmissionAsync). Null means "order not found, outside your
        // scope, or no PDF has been generated yet" — the caller maps that to 404. Served through
        // this authenticated endpoint rather than a static file, since it carries prices.
        Task<(byte[] FileBytes, string FileName)?> GetPdfAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> CreateAsync(Guid warehouseToken, string? notes, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderLineDto?> AddLineAsync(Guid orderToken, Guid articleToken, decimal quantity, decimal? manualUnitPrice, string? manualCurrencyCode, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderLineDto?> EditLineAsync(Guid orderLineToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteLineAsync(Guid orderLineToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> SubmitAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);

        // Creates a new Draft order for the same Warehouse, re-adding every line of a SUBMITTED
        // source order via the existing AddLineAsync (so each line's price is re-resolved fresh,
        // never copied stale). A line whose article can no longer be added (inactive/deleted/
        // superseded/no price) is skipped and reported, never aborting the whole copy — same
        // partial-failure convention as ImportLinesAsync.
        Task<CopyOrderResultDto> CopyAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> CancelAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);

        // Bulk-adds lines to an existing Draft order from an uploaded Excel file — the file
        // is expected to be the export of an OrderTemplate (see IOrderTemplateService.ExportAsync),
        // edited by the user, but doesn't require one to exist. Reuses AddLineAsync per row,
        // never aborting the whole file on one row's failure — same convention as
        // ArticleService.BulkImportArticlesAsync.
        Task<ImportOrderLinesResultDto> ImportLinesAsync(Guid orderToken, byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken);

        // Order approval workflow — see .claude/OrdersModule.md / OrderApprovalModule.md.
        // ApproveAsync auto-completes the submission (creates the PurchaseOrders) the moment
        // every required step for the Order is APPROVED; RejectAsync reverts the Order to DRAFT.
        Task<OrderApprovalStepDto?> ApproveOrderApprovalStepAsync(Guid stepToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderApprovalStepDto?> RejectOrderApprovalStepAsync(Guid stepToken, string reason, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<OrderApprovalStepDto>> GetPendingApprovalStepsAsync(int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
    }
}
