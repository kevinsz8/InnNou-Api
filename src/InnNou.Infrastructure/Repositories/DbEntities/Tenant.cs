using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Tenant
    {
        [Key]
        public int TenantId { get; set; }
        public Guid TenantToken { get; set; } = Guid.NewGuid();

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string Name { get; set; } = default!;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(500)]
        public string Address { get; set; } = default!;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string? CreatedBy { get; set; } = default!;
        public DateTime? LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string? LastUpdatedBy { get; set; } = default!;

    }
}
