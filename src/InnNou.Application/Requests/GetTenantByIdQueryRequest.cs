using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetTenantByIdQueryRequest : IRequest<ApiResponse<GetTenantByIdQueryResponse>>
    {
        public Guid TenantId { get; set; }
        public GetTenantByIdQueryRequest(Guid tenantId)
        {
            TenantId = tenantId;
        }
    }
}
