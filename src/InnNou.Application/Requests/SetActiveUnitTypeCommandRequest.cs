using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record SetActiveUnitTypeCommandRequest(Guid UnitTypeToken, bool IsActive) : IRequest<ApiResponse<SetActiveUnitTypeCommandResponse>>;
}
