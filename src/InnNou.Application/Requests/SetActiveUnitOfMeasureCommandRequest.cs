using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record SetActiveUnitOfMeasureCommandRequest(Guid UnitOfMeasureToken, bool IsActive) : IRequest<ApiResponse<SetActiveUnitOfMeasureCommandResponse>>;
}
