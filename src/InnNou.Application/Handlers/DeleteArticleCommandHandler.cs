using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteArticleCommandHandler(IArticleService articleService, IRequestContext context)
        : IRequestHandler<DeleteArticleCommandRequest, ApiResponse<DeleteArticleCommandResponse>>
    {
        public async Task<ApiResponse<DeleteArticleCommandResponse>> Handle(DeleteArticleCommandRequest request, CancellationToken cancellationToken)
        {
            var deleted = await articleService.DeleteAsync(request.ArticleToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteArticleCommandResponse>.FailureResponse(ErrorCodes.ArticleNotFound, "Article not found.", 404);

            return ApiResponse<DeleteArticleCommandResponse>.SuccessResponse(
                new DeleteArticleCommandResponse { ArticleToken = request.ArticleToken, Success = true }, 200);
        }
    }
}
