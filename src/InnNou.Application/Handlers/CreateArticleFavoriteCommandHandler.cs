using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateArticleFavoriteCommandHandler(IArticleFavoriteService articleFavoriteService, IArticleService articleService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateArticleFavoriteCommandRequest, ApiResponse<CreateArticleFavoriteCommandResponse>>
    {
        public async Task<ApiResponse<CreateArticleFavoriteCommandResponse>> Handle(CreateArticleFavoriteCommandRequest request, CancellationToken cancellationToken)
        {
            var article = await articleService.GetByTokenAsync(request.ArticleToken, context, cancellationToken);
            if (article is null)
                return ApiResponse<CreateArticleFavoriteCommandResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            if (article.ReplacedByArticleId.HasValue)
                return ApiResponse<CreateArticleFavoriteCommandResponse>.FailureResponse(
                    ErrorCodes.ArticleFavoriteArticleReplaced,
                    "This article has been superseded — favorite the replacement article instead.",
                    409);

            var result = await articleFavoriteService.CreateAsync(article.ArticleId, context, cancellationToken);

            return ApiResponse<CreateArticleFavoriteCommandResponse>.SuccessResponse(
                new CreateArticleFavoriteCommandResponse { ArticleFavorite = mapper.Map<Responses.Common.ArticleFavorite>(result) }, 201);
        }
    }
}
