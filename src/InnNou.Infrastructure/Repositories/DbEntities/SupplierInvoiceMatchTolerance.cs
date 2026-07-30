namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // sp_SupplierInvoiceMatchTolerance_GetEffective's row shape — the nearest-organization-wins
    // resolved tolerance, plus whether it came from @OrganizationId itself or an inherited
    // Super Asociado ancestor.
    public class SupplierInvoiceMatchTolerance
    {
        public int SupplierInvoiceMatchToleranceId { get; set; }
        public Guid SupplierInvoiceMatchToleranceToken { get; set; }
        public int EffectiveOrganizationId { get; set; }
        public Guid EffectiveOrganizationToken { get; set; }
        public string? EffectiveOrganizationName { get; set; }
        public decimal TolerancePercent { get; set; }
        public decimal ToleranceAmount { get; set; }
        public bool IsInherited { get; set; }
    }
}
