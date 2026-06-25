using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetUserByTokenQueryResponse
    {
        public User User { get; set; } = default!;
    }
}
