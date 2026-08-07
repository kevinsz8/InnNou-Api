namespace InnNou.Application.Responses
{
    public class AddOrderLinesCommandResponse
    {
        public int TotalLines { get; set; }
        public int SucceededCount { get; set; }
        public int FailureCount { get; set; }
        public List<AddOrderLinesLineError> Errors { get; set; } = new();
    }

    public class AddOrderLinesLineError
    {
        public int Index { get; set; }
        public Guid ArticleToken { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
