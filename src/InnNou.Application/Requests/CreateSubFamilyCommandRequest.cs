using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record CreateSubFamilyCommandRequest(Guid FamilyToken, string Code) : IRequest<ApiResponse<CreateSubFamilyCommandResponse>>;
}
