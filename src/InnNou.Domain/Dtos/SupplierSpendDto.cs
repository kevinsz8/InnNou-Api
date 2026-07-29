namespace InnNou.Domain.Dtos
{
    public class SupplierSpendDto
    {
        public Guid SupplierToken { get; set; }
        public string SupplierName { get; set; } = default!;
        public decimal Total { get; set; }
    }
}
