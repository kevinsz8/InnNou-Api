namespace InnNou.Domain.Dtos
{
    public class ParLevelEffectiveDto
    {
        public decimal BaseMinimumQuantity { get; set; }
        public decimal BaseReorderQuantity { get; set; }

        public decimal EffectiveMinimumQuantity { get; set; }
        public decimal EffectiveReorderQuantity { get; set; }

        // "BASE" / "SEASONAL" / "EVENT".
        public string EffectiveSource { get; set; } = default!;
        public Guid? EffectiveOverrideToken { get; set; }
        public string? EffectiveOverrideLabel { get; set; }
    }
}
