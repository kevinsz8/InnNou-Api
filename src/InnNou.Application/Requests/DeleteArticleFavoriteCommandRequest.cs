using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteArticleFavoriteCommandRequest : IRequest<ApiResponse<DeleteArticleFavoriteCommandResponse>>
    {
        public Guid ArticleToken { get; set; }
    }
}
