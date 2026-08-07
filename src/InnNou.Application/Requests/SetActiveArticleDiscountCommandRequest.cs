using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveArticleDiscountCommandRequest : IRequest<ApiResponse<SetActiveArticleDiscountCommandResponse>>
    {
        public Guid ArticleDiscountToken { get; set; }
        public bool IsActive { get; set; }
    }
}
