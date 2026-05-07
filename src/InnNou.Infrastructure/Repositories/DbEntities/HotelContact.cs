using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class HotelContact
    {
        [Key]
        public int HotelContactId { get; set; }

        public Guid HotelContactToken { get; set; } = Guid.NewGuid();

        public int HotelId { get; set; }

        [MaxLength(150)]
        public string Name { get; set; } = default!;

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? Mobile { get; set; }

        [MaxLength(50)]
        public string? Fax { get; set; }

        public bool IsPrimary { get; set; } = false;

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
