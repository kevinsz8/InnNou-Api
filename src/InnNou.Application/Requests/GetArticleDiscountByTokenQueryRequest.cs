using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetArticleDiscountByTokenQueryRequest : IRequest<ApiResponse<GetArticleDiscountByTokenQueryResponse>>
    {
        public Guid ArticleDiscountToken { get; set; }
    }
}
