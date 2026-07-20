using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveZoneCommandRequest : IRequest<ApiResponse<SetActiveZoneCommandResponse>>
    {
        public Guid ZoneToken { get; set; }
        public bool IsActive { get; set; }
    }
}
