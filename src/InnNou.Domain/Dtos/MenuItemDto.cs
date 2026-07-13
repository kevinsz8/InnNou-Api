namespace InnNou.Domain.Dtos
{
    public class MenuItemDto
    {
        public int MenuItemId { get; set; }
        public Guid MenuItemToken { get; set; }
        public Guid? ParentMenuItemToken { get; set; }
        public string Name { get; set; } = default!;
        public string? Route { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
    }
}
