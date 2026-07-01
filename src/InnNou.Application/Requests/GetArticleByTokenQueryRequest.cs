using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetArticleByTokenQueryRequest : IRequest<ApiResponse<GetArticleByTokenQueryResponse>>
    {
        public Guid ArticleToken { get; set; }
    }
}
