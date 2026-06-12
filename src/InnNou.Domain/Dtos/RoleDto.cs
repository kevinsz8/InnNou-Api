namespace InnNou.Domain.Dtos
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public Guid RoleToken { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public string? Description { get; set; }
        public int RoleLevel { get; set; }
        public bool CanImpersonate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
