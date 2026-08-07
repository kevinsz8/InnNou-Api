using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IArticleDiscountService
    {
        Task<PagedResult<ArticleDiscountDto>> GetPagedAsync(Guid supplierToken, int pageNumber, int pageSize, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticleDiscountDto?> GetByTokenAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticleDiscountDto?> CreateAsync(Guid supplierToken, Guid? articleToken, Guid? subFamilyToken, Guid? familyToken, string discountTypeCode, decimal discountValue, string? currencyCode, DateTime effectiveFrom, DateTime? effectiveUntil, string? description, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticleDiscountDto?> EditAsync(Guid token, string discountTypeCode, decimal discountValue, string? currencyCode, DateTime effectiveFrom, DateTime? effectiveUntil, string? description, IRequestContext context, CancellationToken cancellationToken = default);
        Task<ArticleDiscountDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
