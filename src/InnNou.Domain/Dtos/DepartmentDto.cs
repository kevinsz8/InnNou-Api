namespace InnNou.Domain.Dtos
{
    public class DepartmentDto
    {
        public Guid DepartmentToken { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }

        public string Name { get; set; } = default!;
        public string? Code { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
