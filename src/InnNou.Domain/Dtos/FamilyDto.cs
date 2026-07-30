namespace InnNou.Domain.Dtos
{
    public class FamilyDto
    {
        public int FamilyId { get; set; }
        public Guid FamilyToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }

        // Write-in bridge (resolved to DefaultTaxCategoryId inside FamilyService.SetDefaultTaxCategoryAsync)
        // AND denormalized read-only display value on a hydrated read, same dual-purpose pattern as
        // WarehouseDto.ZoneToken.
        public int? DefaultTaxCategoryId { get; set; }
        public Guid? DefaultTaxCategoryToken { get; set; }
        public string? DefaultTaxCategoryCode { get; set; }
    }
}
