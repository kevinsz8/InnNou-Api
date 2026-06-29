using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetUnitConversionRateByTokenQueryRequest : IRequest<ApiResponse<GetUnitConversionRateByTokenQueryResponse>>
    {
        public Guid UnitConversionRateToken { get; set; }
    }
}
