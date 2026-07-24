namespace InnNou.Application.Responses.Common
{
    public class ParLevelEffective
    {
        public decimal BaseMinimumQuantity { get; set; }
        public decimal BaseReorderQuantity { get; set; }
        public decimal EffectiveMinimumQuantity { get; set; }
        public decimal EffectiveReorderQuantity { get; set; }
        public string EffectiveSource { get; set; } = default!;
        public Guid? EffectiveOverrideToken { get; set; }
        public string? EffectiveOverrideLabel { get; set; }
    }
}
