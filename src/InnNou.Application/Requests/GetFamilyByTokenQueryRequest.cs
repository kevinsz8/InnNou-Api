using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public record GetFamilyByTokenQueryRequest(Guid FamilyToken) : IRequest<ApiResponse<GetFamilyByTokenQueryResponse>>;
}
