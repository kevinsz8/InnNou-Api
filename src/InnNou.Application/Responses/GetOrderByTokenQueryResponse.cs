using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetOrderByTokenQueryResponse
    {
        public Order Order { get; set; } = default!;
    }
}
