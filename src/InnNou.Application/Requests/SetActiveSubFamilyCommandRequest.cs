using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record SetActiveSubFamilyCommandRequest(Guid SubFamilyToken, bool IsActive) : IRequest<ApiResponse<SetActiveSubFamilyCommandResponse>>;
}
