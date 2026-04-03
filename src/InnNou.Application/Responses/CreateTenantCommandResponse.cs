using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class CreateTenantCommandResponse
    {
        public Tenant Tenant { get; set; } = default!;
    }
}
