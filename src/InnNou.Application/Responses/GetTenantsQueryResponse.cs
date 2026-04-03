using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetTenantsQueryResponse
    {
        public List<Tenant> Tenants { get; set; } = new();
    }
}
