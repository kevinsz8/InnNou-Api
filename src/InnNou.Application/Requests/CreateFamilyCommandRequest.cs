using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record CreateFamilyCommandRequest(string Code) : IRequest<ApiResponse<CreateFamilyCommandResponse>>;
}
