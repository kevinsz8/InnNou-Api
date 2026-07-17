using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IOrderService
    {
        Task<PagedResult<OrderDto>> GetPagedAsync(Guid? warehouseToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> GetByTokenAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> CreateAsync(Guid warehouseToken, string? notes, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderLineDto?> AddLineAsync(Guid orderToken, Guid articleToken, decimal quantity, decimal? manualUnitPrice, string? manualCurrencyCode, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderLineDto?> EditLineAsync(Guid orderLineToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteLineAsync(Guid orderLineToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> SubmitAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> CancelAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);

        // Bulk-adds lines to an existing Draft order from an uploaded Excel file — the file
        // is expected to be the export of an OrderTemplate (see IOrderTemplateService.ExportAsync),
        // edited by the user, but doesn't require one to exist. Reuses AddLineAsync per row,
        // never aborting the whole file on one row's failure — same convention as
        // ArticleService.BulkImportArticlesAsync.
        Task<ImportOrderLinesResultDto> ImportLinesAsync(Guid orderToken, byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken);
    }
}
