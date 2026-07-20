namespace InnNou.Domain.Dtos
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public Guid CategoryToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }

        // NULL = global/system category. Set = anchored to exactly one Super
        // Asociado organization, immutable after create.
        public int? OrganizationId { get; set; }

        // Write-only input on Create: the target owning organization. SuperAdmin may
        // set this to any organization (or omit for global); a Super Asociado's own
        // Staff+ has it ignored in favor of their own context.OrganizationId. Never
        // round-trips in a response mapping — see OrganizationTokenResult for that.
        public Guid? OrganizationToken { get; set; }

        // Read-only, denormalized display fields — populated only when OrganizationId
        // is set. Same split as SupplierDto's OrganizationToken/OrganizationTokenResult.
        public Guid? OrganizationTokenResult { get; set; }
        public string? OrganizationName { get; set; }
    }
}
