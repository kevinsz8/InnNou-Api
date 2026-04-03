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
        public string FirstName { get; set; } = default!;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string LastName { get; set; } = default!;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string Email { get; set; } = default!;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string UserName { get; set; } = default!;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = default!;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string? CreatedBy { get; set; } = default!;
        public DateTime? LastUpdatedUtc { get; set;} = DateTime.UtcNow;

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string? LastUpdatedBy { get; set; } = default!;

    }
}
