using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record CreateUnitTypeCommandRequest(string Code) : IRequest<ApiResponse<CreateUnitTypeCommandResponse>>;
}
