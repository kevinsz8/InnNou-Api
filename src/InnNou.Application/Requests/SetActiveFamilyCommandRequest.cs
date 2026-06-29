using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SetActiveFamilyCommandRequest : IRequest<ApiResponse<SetActiveFamilyCommandResponse>>
    {
        public Guid FamilyToken { get; set; }
        public bool IsActive { get; set; }
    }
}
