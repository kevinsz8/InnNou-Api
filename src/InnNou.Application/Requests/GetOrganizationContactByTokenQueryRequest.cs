using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetOrganizationContactByTokenQueryRequest : IRequest<ApiResponse<GetOrganizationContactByTokenQueryResponse>>
    {
        public Guid OrganizationContactToken { get; set; }
    }
}
