using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class SetActiveArticleCommandResponse
    {
        public Article Article { get; set; } = default!;
    }
}
