using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteArticleCommandRequest : IRequest<ApiResponse<DeleteArticleCommandResponse>>
    {
        public Guid ArticleToken { get; set; }
    }
}
