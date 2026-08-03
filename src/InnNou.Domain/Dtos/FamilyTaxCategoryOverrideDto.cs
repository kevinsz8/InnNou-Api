namespace InnNou.Domain.Dtos
{
    public class FamilyTaxCategoryOverrideDto
    {
        public Guid FamilyTaxCategoryOverrideToken { get; set; }
        public bool IsActive { get; set; }
        public Guid TaxJurisdictionToken { get; set; }
        public string TaxJurisdictionCode { get; set; } = default!;
        public string TaxJurisdictionName { get; set; } = default!;
        public Guid TaxCategoryToken { get; set; }
        public string TaxCategoryCode { get; set; } = default!;
    }
}
