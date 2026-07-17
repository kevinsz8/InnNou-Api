using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ApplyOrderTemplateCommandRequest : IRequest<ApiResponse<ApplyOrderTemplateCommandResponse>>
    {
        public Guid OrderTemplateToken { get; set; }
        public Guid OrderToken { get; set; }
    }
}
