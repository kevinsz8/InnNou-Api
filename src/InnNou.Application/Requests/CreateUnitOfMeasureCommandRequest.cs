using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record CreateUnitOfMeasureCommandRequest(
        Guid UnitTypeToken,
        string Code,
        string Symbol,
        int Decimals) : IRequest<ApiResponse<CreateUnitOfMeasureCommandResponse>>;
}
