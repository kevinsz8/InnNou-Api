namespace InnNou.Domain.Dtos
{
    public class EligibleReturnLineDto
    {
        public Guid GoodsReceiptLineToken { get; set; }
        public string? DeliveryNoteNumber { get; set; }
        public DateTime ReceivedUtc { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
    }
}
