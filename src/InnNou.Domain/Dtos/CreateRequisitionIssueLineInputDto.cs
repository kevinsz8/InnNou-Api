namespace InnNou.Domain.Dtos
{
    // One line issued as part of a single RequisitionIssue create call. QuantityIssued must be >
    // 0 and can never exceed what's still outstanding on the referenced RequisitionLine
    // (QuantityRequested - QuantityIssued so far) — validated server-side, same negative/over-
    // issue guard shape as GoodsReceipt's own QuantityAccepted cap.
    public class CreateRequisitionIssueLineInputDto
    {
        public Guid RequisitionLineToken { get; set; }
        public decimal QuantityIssued { get; set; }
        public string? Notes { get; set; }
    }
}
