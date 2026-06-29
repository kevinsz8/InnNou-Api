using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetUnitOfMeasureByTokenQueryRequest : IRequest<ApiResponse<GetUnitOfMeasureByTokenQueryResponse>>
    {
        public Guid UnitOfMeasureToken { get; set; }
    }
}
