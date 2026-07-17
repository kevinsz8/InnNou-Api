using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditOrderTemplateLineCommandRequest : IRequest<ApiResponse<EditOrderTemplateLineCommandResponse>>
    {
        public Guid OrderTemplateLineToken { get; set; }
        public decimal Quantity { get; set; }
    }
}
