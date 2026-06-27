using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record SetActiveUnitConversionRateCommandRequest(Guid UnitConversionRateToken, bool IsActive) : IRequest<ApiResponse<SetActiveUnitConversionRateCommandResponse>>;
}
