using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveArticleCommandRequest : IRequest<ApiResponse<SetActiveArticleCommandResponse>>
    {
        public Guid ArticleToken { get; set; }
        public bool IsActive { get; set; }
    }
}
