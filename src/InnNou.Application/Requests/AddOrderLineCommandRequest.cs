using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class AddOrderLineCommandRequest : IRequest<ApiResponse<AddOrderLineCommandResponse>>
    {
        public Guid OrderToken { get; set; }
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }
    }
}
