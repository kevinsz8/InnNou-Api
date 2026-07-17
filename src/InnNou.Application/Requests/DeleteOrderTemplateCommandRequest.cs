using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteOrderTemplateCommandRequest : IRequest<ApiResponse<DeleteOrderTemplateCommandResponse>>
    {
        public Guid OrderTemplateToken { get; set; }
    }
}
