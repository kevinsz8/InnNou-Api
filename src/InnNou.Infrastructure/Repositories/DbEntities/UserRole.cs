using System.ComponentModel.DataAnnotations;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class UserRole
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

    }
}
