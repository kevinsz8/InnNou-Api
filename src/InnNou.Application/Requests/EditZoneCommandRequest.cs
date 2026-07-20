using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditZoneCommandRequest : IRequest<ApiResponse<EditZoneCommandResponse>>
    {
        public Guid ZoneToken { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
