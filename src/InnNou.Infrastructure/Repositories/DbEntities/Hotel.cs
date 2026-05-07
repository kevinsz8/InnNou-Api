using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Hotel
    {
        [Key]
        public int HotelId { get; set; }

        public Guid HotelToken { get; set; } = Guid.NewGuid();

        [MaxLength(200)]
        public string Name { get; set; } = default!;

        [MaxLength(200)]
        public string? LegalName { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        public int? ParentHotelId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string? LastUpdatedBy { get; set; }
    }
}
