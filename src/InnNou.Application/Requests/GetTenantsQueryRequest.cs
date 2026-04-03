using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetTenantsQueryRequest : IRequest<ApiResponse<GetTenantsQueryResponse>>
    {
    }
}
