namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }
        public Guid MenuItemToken { get; set; }
        public int? ParentMenuItemId { get; set; }
        public string Name { get; set; } = default!;
        public string? Route { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
    }
}
