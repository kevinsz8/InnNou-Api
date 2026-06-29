using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveSubFamilyCommandRequest : IRequest<ApiResponse<SetActiveSubFamilyCommandResponse>>
    {
        public Guid SubFamilyToken { get; set; }
        public bool IsActive { get; set; }
    }
}
