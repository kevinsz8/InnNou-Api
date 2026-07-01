using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetOrganizationByTokenQueryResponse
    {
        public Organization Organization { get; set; } = default!;
    }
}
