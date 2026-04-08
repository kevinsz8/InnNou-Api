namespace InnNou.Domain.Dtos
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string Name { get; set; } = default!;
        public int Level { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
