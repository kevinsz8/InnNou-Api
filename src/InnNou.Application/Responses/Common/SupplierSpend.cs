namespace InnNou.Application.Responses.Common
{
    public class SupplierSpend
    {
        public Guid SupplierToken { get; set; }
        public string SupplierName { get; set; } = default!;
        public decimal Total { get; set; }
    }
}
