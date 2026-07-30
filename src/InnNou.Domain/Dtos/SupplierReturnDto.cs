namespace InnNou.Domain.Dtos
{
    public class SupplierReturnDto
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

        // Populated by SupplierReturnService via sp_SupplierReturnLine_GetBySupplierReturnId —
        // the individual rejected GoodsReceiptLines this case is claiming.
        public List<SupplierReturnLineDto> Lines { get; set; } = [];
    }
}
