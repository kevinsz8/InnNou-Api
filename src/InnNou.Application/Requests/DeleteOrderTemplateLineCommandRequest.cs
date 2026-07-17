using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteOrderTemplateLineCommandRequest : IRequest<ApiResponse<DeleteOrderTemplateLineCommandResponse>>
    {
        public Guid OrderTemplateLineToken { get; set; }
    }
}
