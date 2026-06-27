using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record GetSubFamilyByTokenQueryRequest(Guid SubFamilyToken) : IRequest<ApiResponse<GetSubFamilyByTokenQueryResponse>>;
}
