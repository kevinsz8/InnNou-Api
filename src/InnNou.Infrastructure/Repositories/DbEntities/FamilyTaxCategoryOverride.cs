namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class FamilyTaxCategoryOverride
    {
        public int FamilyTaxCategoryOverrideId { get; set; }
        public Guid FamilyTaxCategoryOverrideToken { get; set; }
        public int FamilyId { get; set; }
        public bool IsActive { get; set; }
        public int TaxJurisdictionId { get; set; }
        public Guid TaxJurisdictionToken { get; set; }
        public string TaxJurisdictionCode { get; set; } = default!;
        public string TaxJurisdictionName { get; set; } = default!;
        public int TaxCategoryId { get; set; }
        public Guid TaxCategoryToken { get; set; }
        public string TaxCategoryCode { get; set; } = default!;
    }
}
