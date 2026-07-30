namespace InnNou.Domain.Dtos
{
    public class SupplierInvoiceMatchToleranceDto
    {
        public Guid SupplierInvoiceMatchToleranceToken { get; set; }
        public Guid EffectiveOrganizationToken { get; set; }
        public string? EffectiveOrganizationName { get; set; }
        public decimal TolerancePercent { get; set; }
        public decimal ToleranceAmount { get; set; }
        public bool IsInherited { get; set; }
    }
}
