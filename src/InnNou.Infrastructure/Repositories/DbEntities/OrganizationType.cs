namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class OrganizationType
    {
        public int OrganizationTypeId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
