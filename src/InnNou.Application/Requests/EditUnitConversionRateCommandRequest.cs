using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record EditUnitConversionRateCommandRequest(Guid UnitConversionRateToken, decimal Factor) : IRequest<ApiResponse<EditUnitConversionRateCommandResponse>>;
}
