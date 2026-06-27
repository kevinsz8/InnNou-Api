using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record EditUnitOfMeasureCommandRequest(
        Guid UnitOfMeasureToken,
        string Code,
        string Symbol,
        int Decimals) : IRequest<ApiResponse<EditUnitOfMeasureCommandResponse>>;
}
