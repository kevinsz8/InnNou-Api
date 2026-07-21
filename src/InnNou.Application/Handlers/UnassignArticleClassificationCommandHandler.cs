using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class UnassignArticleClassificationCommandHandler(IArticleClassificationService articleClassificationService, IArticleService articleService, IRequestContext context)
        : IRequestHandler<UnassignArticleClassificationCommandRequest, ApiResponse<UnassignArticleClassificationCommandResponse>>
    {
        public async Task<ApiResponse<UnassignArticleClassificationCommandResponse>> Handle(UnassignArticleClassificationCommandRequest request, CancellationToken cancellationToken)
        {
            var article = await articleService.GetByTokenAsync(request.ArticleToken, context, cancellationToken);
            if (article is null)
                return ApiResponse<UnassignArticleClassificationCommandResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            var unassigned = await articleClassificationService.UnassignAsync(article.ArticleId, request.OrganizationToken, context, cancellationToken);
            if (!unassigned)
                return ApiResponse<UnassignArticleClassificationCommandResponse>.FailureResponse(
                    ErrorCodes.ArticleClassificationNotFound,
                    "This article is not classified by your organization directly (it may only be inherited from a parent organization, which only that organization can remove).",
                    404);

            return ApiResponse<UnassignArticleClassificationCommandResponse>.SuccessResponse(new UnassignArticleClassificationCommandResponse
            {
                ArticleToken = request.ArticleToken,
                Success = true
            });
        }
    }
}
