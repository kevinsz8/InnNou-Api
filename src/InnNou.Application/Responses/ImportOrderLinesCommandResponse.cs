namespace InnNou.Application.Responses
{
    public class ImportOrderLinesCommandResponse
    {
        public int TotalRows { get; set; }
        public int SucceededCount { get; set; }
        public int FailureCount { get; set; }
        public List<ImportOrderLinesRowError> Errors { get; set; } = new();
    }

    public class ImportOrderLinesRowError
    {
        public int RowNumber { get; set; }
        public string? Identifier { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
