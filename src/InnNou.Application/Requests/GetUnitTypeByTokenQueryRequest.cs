using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record GetUnitTypeByTokenQueryRequest(Guid UnitTypeToken) : IRequest<ApiResponse<GetUnitTypeByTokenQueryResponse>>;
}
