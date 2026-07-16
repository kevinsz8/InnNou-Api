using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateArticleFavoriteCommandRequest : IRequest<ApiResponse<CreateArticleFavoriteCommandResponse>>
    {
        public Guid ArticleToken { get; set; }
    }
}
