using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record SetActiveFamilyCommandRequest(Guid FamilyToken, bool IsActive) : IRequest<ApiResponse<SetActiveFamilyCommandResponse>>;
}
