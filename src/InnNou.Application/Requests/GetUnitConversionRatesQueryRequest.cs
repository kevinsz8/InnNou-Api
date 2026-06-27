using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetUnitConversionRatesQueryRequest : IRequest<ApiResponse<GetUnitConversionRatesQueryResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? UnitTypeToken { get; set; }
    }
}
