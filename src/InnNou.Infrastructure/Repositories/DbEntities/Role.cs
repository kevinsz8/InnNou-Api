namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Role
    {
        public int RoleId { get; set; }
        public Guid RoleToken { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public string? Description { get; set; }
        public int RoleLevel { get; set; }
        public bool CanImpersonate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
