namespace InnNou.Application.Responses.Common
{
    public class SupplierReturn
    {
        public Guid SupplierReturnToken { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public Guid SupplierToken { get; set; }
        public string? SupplierName { get; set; }
        public string Status { get; set; } = default!;
        public string? ResolutionType { get; set; }
        public string? Notes { get; set; }
        public DateTime? ClosedUtc { get; set; }
        public string? ClosedBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public int LineCount { get; set; }
        public List<SupplierReturnLine> Lines { get; set; } = [];
    }
}
