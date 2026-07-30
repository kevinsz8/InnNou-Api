namespace InnNou.Application.Responses.Common
{
    public class TaxRateGridRow
    {
        public Guid TaxJurisdictionToken { get; set; }
        public string TaxJurisdictionCode { get; set; } = default!;
        public string TaxJurisdictionName { get; set; } = default!;
        public Guid TaxCategoryToken { get; set; }
        public string TaxCategoryCode { get; set; } = default!;
        public Guid? TaxRateToken { get; set; }
        public decimal? RatePercent { get; set; }
    }
}
