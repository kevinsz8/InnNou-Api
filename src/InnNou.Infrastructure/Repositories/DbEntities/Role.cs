using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Column(TypeName = "VARCHAR")]
        [MaxLength(150)]
        public string Name { get; set; } = default!; // "Owner", "Admin", "FrontDesk", etc.

    }
}
