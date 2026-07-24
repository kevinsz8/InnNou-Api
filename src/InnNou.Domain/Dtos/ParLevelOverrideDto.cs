namespace InnNou.Domain.Dtos
{
    public class ParLevelOverrideDto
    {
        public Guid ParLevelOverrideToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }

        // "SEASONAL" / "EVENT".
        public string Type { get; set; } = default!;
        public string? Label { get; set; }

        public decimal MinimumQuantity { get; set; }
        public decimal ReorderQuantity { get; set; }

        // SEASONAL only (recurring, no year).
        public int? StartMonth { get; set; }
        public int? StartDay { get; set; }
        public int? EndMonth { get; set; }
        public int? EndDay { get; set; }

        // EVENT only (literal one-off dates).
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
