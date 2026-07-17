namespace InnNou.Domain.Dtos
{
    public class ImportOrderLinesResultDto
    {
        public int TotalRows { get; set; }
        public int SucceededCount { get; set; }
        public int FailureCount { get; set; }
        public List<ImportOrderLinesRowErrorDto> Errors { get; set; } = new();
    }

    public class ImportOrderLinesRowErrorDto
    {
        public int RowNumber { get; set; }
        public string? Identifier { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
