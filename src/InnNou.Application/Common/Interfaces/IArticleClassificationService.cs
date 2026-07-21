using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface IArticleClassificationService
    {
        Task<ArticleClassificationDto> AssignAsync(int articleId, int categoryId, int? subCategoryId, Guid? organizationToken, IRequestContext context, CancellationToken cancellationToken = default);
        Task<bool> UnassignAsync(int articleId, Guid? organizationToken, IRequestContext context, CancellationToken cancellationToken = default);
        Task<BulkAssignArticleClassificationResultDto> BulkAssignAsync(List<int> articleIds, int categoryId, int? subCategoryId, Guid? organizationToken, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
