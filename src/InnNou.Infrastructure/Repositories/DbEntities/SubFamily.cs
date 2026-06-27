namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SubFamily
    {
        public int SubFamilyId { get; set; }
        public Guid SubFamilyToken { get; set; }
        public int FamilyId { get; set; }
        public string Code { get; set; } = default!;
        public bool IsSystem { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
