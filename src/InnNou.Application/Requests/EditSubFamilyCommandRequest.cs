using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record EditSubFamilyCommandRequest(Guid SubFamilyToken, string Code) : IRequest<ApiResponse<EditSubFamilyCommandResponse>>;
}
