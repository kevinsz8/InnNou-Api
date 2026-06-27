using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record EditFamilyCommandRequest(Guid FamilyToken, string Code) : IRequest<ApiResponse<EditFamilyCommandResponse>>;
}
