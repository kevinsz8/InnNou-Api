using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IArticlePriceService
    {
        Task<ArticlePriceDto?> CreateAsync(ArticlePriceDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticlePriceDto?> GetCurrentAsync(int articleId, int supplierId, int? requestedOrganizationId, string? currencyCode, DateTime asOfDate, IRequestContext context, CancellationToken cancellationToken = default);
        Task<PagedResult<ArticlePriceDto>> GetHistoryAsync(int pageNumber, int pageSize, int articleId, int supplierId, int? requestedOrganizationId, string? currencyCode, IRequestContext context, CancellationToken cancellationToken = default);
        Task<BulkImportArticlePriceResultDto> BulkImportArticlePricesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> ExportArticlePricesAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> GenerateArticlePriceImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
