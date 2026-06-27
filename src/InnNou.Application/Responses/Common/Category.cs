namespace InnNou.Application.Responses.Common
{
    public class Category
    {
        public Guid CategoryToken { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
    }
}
