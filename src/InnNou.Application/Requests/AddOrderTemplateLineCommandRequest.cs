using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class AddOrderTemplateLineCommandRequest : IRequest<ApiResponse<AddOrderTemplateLineCommandResponse>>
    {
        public Guid OrderTemplateToken { get; set; }
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }
    }
}
