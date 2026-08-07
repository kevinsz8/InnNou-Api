namespace InnNou.Domain.Dtos
{
    // Best-effort batch-add-lines result — mirrors ImportOrderLinesResultDto's shape (one line's
    // failure never aborts the rest), but keyed by the request array's own Index rather than a
    // spreadsheet RowNumber.
    public class AddOrderLinesResultDto
    {
        public int TotalLines { get; set; }
        public int SucceededCount { get; set; }
        public int FailureCount { get; set; }
        public List<AddOrderLinesLineErrorDto> Errors { get; set; } = new();
    }

    public class AddOrderLinesLineErrorDto
    {
        public int Index { get; set; }
        public Guid ArticleToken { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
