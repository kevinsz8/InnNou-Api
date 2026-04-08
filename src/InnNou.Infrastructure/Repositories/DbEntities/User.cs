using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public Guid UserToken { get; set; } = Guid.NewGuid();

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string FirstName { get; set; }

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string LastName { get; set; }

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string Email { get; set; }

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string UserName { get; set; }

        [Column(TypeName = "VARCHAR")]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = default!;

        public int RoleId { get; set; }
        public int? HotelId { get; set; }
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
