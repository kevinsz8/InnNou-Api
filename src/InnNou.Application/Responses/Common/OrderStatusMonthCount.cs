namespace InnNou.Application.Responses.Common
{
    public class OrderStatusMonthCount
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string StatusCode { get; set; } = default!;
        public int Count { get; set; }
    }
}
