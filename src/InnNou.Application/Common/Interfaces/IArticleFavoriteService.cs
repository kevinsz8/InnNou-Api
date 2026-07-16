using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IArticleFavoriteService
    {
        Task<ArticleFavoriteDto> CreateAsync(int articleId, IRequestContext context, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int articleId, IRequestContext context, CancellationToken cancellationToken = default);
        Task<PagedResult<ArticleFavoriteDto>> GetEffectiveAsync(int pageNumber, int pageSize, Guid? organizationToken, string? searchText, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
