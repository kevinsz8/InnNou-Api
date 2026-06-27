using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record GetUnitConversionRatesQueryRequest(Guid? UnitTypeToken = null) : IRequest<ApiResponse<GetUnitConversionRatesQueryResponse>>;
}
