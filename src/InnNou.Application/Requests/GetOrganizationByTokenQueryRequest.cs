using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetOrganizationByTokenQueryRequest : IRequest<ApiResponse<GetOrganizationByTokenQueryResponse>>
    {
        public Guid OrganizationToken { get; set; }
    }
}
