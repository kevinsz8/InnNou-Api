using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class ApplyOrderTemplateCommandResponse
    {
        public Guid OrderTemplateToken { get; set; }
        public Guid OrderToken { get; set; }
        public int TotalLines { get; set; }
        public int SucceededCount { get; set; }
        public int NeedsManualPriceCount { get; set; }
        public int FailedCount { get; set; }
        public List<ApplyOrderTemplateLineResult> Lines { get; set; } = new();
    }
}
