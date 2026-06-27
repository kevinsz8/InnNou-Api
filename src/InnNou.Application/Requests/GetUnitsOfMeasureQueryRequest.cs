using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetUnitsOfMeasureQueryRequest : IRequest<ApiResponse<GetUnitsOfMeasureQueryResponse>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? UnitTypeToken { get; set; }
    }
}
