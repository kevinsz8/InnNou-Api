using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record GetUnitTypesQueryRequest : IRequest<ApiResponse<GetUnitTypesQueryResponse>>;
}
