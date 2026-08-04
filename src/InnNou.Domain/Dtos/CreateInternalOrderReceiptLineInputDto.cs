namespace InnNou.Domain.Dtos
{
    // One line closed out as part of a single InternalOrderReceipt create call — a 2-way
    // Accepted/Rejected split (no Courtesy, see the schema migration header note for why).
    // QuantityAccepted is capped against the referenced InternalOrderShipmentLine's remaining-to-
    // receive (QuantityShipped - already-received total); RejectionReason is required whenever
    // QuantityRejected > 0.
    public class CreateInternalOrderReceiptLineInputDto
    {
        public Guid InternalOrderShipmentLineToken { get; set; }
        public decimal QuantityAccepted { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
    }
}
