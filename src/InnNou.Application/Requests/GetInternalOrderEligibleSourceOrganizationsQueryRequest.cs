using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    // No params — always the caller's own nearest Super Asociado's peer ASSOCIATE organizations.
    public class GetInternalOrderEligibleSourceOrganizationsQueryRequest : IRequest<ApiResponse<GetInternalOrderEligibleSourceOrganizationsQueryResponse>>
    {
    }
}
