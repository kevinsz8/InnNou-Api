using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class RenameOrderTemplateCommandRequest : IRequest<ApiResponse<RenameOrderTemplateCommandResponse>>
    {
        public Guid OrderTemplateToken { get; set; }
        public string Name { get; set; } = default!;
    }
}
