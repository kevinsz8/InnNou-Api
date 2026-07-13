namespace InnNou.Application.Responses.Common
{
    public class MenuItem
    {
        public Guid MenuItemToken { get; set; }
        public Guid? ParentMenuItemToken { get; set; }
        public string Name { get; set; } = default!;
        public string? Route { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }
    }
}
