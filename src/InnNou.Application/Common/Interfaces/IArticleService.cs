using InnNou.Application.Common;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IArticleService
    {
        Task<PagedResult<ArticleDto>> GetPagedAsync(int pageNumber, int pageSize, int? supplierId, int? familyId, int? subFamilyId, string? searchText, bool includeInactive, bool favoritesOnly, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticleDto?> GetByTokenAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default);
        Task<bool> ExistsBySupplierSkuAsync(int supplierId, string supplierSku, Guid? excludeToken, CancellationToken cancellationToken = default);
        Task<ArticleDto?> CreateAsync(ArticleDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticleDto?> EditAsync(ArticleDto dto, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticleDto?> SupersedeAsync(Guid oldArticleToken, ArticleDto newArticleData, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticleDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default);
        Task<BulkImportArticleResultDto> BulkImportArticlesAsync(byte[] fileBytes, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> ExportArticlesAsync(string? searchText, bool includeInactive, string? language, IRequestContext context, CancellationToken cancellationToken = default);
        Task<(byte[] FileBytes, string FileName)> GenerateArticleImportTemplateAsync(string? language, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
