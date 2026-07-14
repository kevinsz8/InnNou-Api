using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveArticleCommandHandler(IArticleService articleService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SetActiveArticleCommandRequest, ApiResponse<SetActiveArticleCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveArticleCommandResponse>> Handle(SetActiveArticleCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await articleService.SetActiveAsync(request.ArticleToken, request.IsActive, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveArticleCommandResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            var response = new SetActiveArticleCommandResponse { Article = mapper.Map<Responses.Common.Article>(result) };
            return ApiResponse<SetActiveArticleCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
