using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetTenantByIdQueryResponse
    {
        public Tenant Tenant { get; set; } = default!;
    }
}
