namespace InnNou.Domain.Dtos
{
    public class OrderStatusMonthCountDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string StatusCode { get; set; } = default!;
        public int Count { get; set; }
    }
}
