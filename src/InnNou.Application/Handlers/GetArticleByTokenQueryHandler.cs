using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetArticleByTokenQueryHandler(IArticleService articleService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetArticleByTokenQueryRequest, ApiResponse<GetArticleByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetArticleByTokenQueryResponse>> Handle(GetArticleByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var article = await articleService.GetByTokenAsync(request.ArticleToken, context, cancellationToken);
            if (article is null)
                return ApiResponse<GetArticleByTokenQueryResponse>.FailureResponse("ARTICLE_NOT_FOUND", "Article not found.", 404);

            return ApiResponse<GetArticleByTokenQueryResponse>.SuccessResponse(
                new GetArticleByTokenQueryResponse { Article = mapper.Map<Responses.Common.Article>(article) }, 200);
        }
    }
}
