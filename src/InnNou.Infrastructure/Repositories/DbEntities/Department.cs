namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public Guid DepartmentToken { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }

        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public string? Code { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
