namespace InnNou.Domain.Dtos
{
    public class ApplyOrderTemplateResultDto
    {
        public Guid OrderTemplateToken { get; set; }
        public Guid OrderToken { get; set; }
        public int TotalLines { get; set; }
        public int SucceededCount { get; set; }
        public int NeedsManualPriceCount { get; set; }
        public int FailedCount { get; set; }
        public List<ApplyOrderTemplateLineResultDto> Lines { get; set; } = [];
    }
}
