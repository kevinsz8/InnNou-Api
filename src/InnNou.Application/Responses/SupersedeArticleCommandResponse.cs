using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class SupersedeArticleCommandResponse
    {
        public Guid ReplacedArticleToken { get; set; }
        public Article? Article { get; set; }
    }
}
