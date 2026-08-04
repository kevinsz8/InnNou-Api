namespace InnNou.Domain.Dtos
{
    public class InternalOrderReceiptDto
    {
        public Guid InternalOrderReceiptToken { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Populated by InternalOrderService via sp_InternalOrderReceiptLine_GetByInternalOrderReceiptId.
        public List<InternalOrderReceiptLineDto> Lines { get; set; } = [];
    }
}
