using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetUnitTypeByTokenQueryRequest : IRequest<ApiResponse<GetUnitTypeByTokenQueryResponse>>
    {
        public Guid UnitTypeToken { get; set; }
    }
}
