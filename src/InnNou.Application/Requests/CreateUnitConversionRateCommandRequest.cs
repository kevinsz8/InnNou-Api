using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record CreateUnitConversionRateCommandRequest(
        Guid FromUnitOfMeasureToken,
        Guid ToUnitOfMeasureToken,
        decimal Factor) : IRequest<ApiResponse<CreateUnitConversionRateCommandResponse>>;
}
