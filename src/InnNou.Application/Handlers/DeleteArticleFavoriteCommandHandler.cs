using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteArticleFavoriteCommandHandler(IArticleFavoriteService articleFavoriteService, IArticleService articleService, IRequestContext context)
        : IRequestHandler<DeleteArticleFavoriteCommandRequest, ApiResponse<DeleteArticleFavoriteCommandResponse>>
    {
        public async Task<ApiResponse<DeleteArticleFavoriteCommandResponse>> Handle(DeleteArticleFavoriteCommandRequest request, CancellationToken cancellationToken)
        {
            var article = await articleService.GetByTokenAsync(request.ArticleToken, context, cancellationToken);
            if (article is null)
                return ApiResponse<DeleteArticleFavoriteCommandResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            var deleted = await articleFavoriteService.DeleteAsync(article.ArticleId, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteArticleFavoriteCommandResponse>.FailureResponse(
                    ErrorCodes.ArticleFavoriteNotFound,
                    "This article is not one of your organization's own favorites (it may only be inherited from a parent organization, which only that organization can remove).",
                    404);

            return ApiResponse<DeleteArticleFavoriteCommandResponse>.SuccessResponse(new DeleteArticleFavoriteCommandResponse
            {
                ArticleToken = request.ArticleToken,
                Success = true
            });
        }
    }
}
