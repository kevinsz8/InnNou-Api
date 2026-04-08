using System.ComponentModel.DataAnnotations;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Hotel
    {
        [Key]
        public int HotelId { get; set; }

        public Guid HotelToken { get; set; } = Guid.NewGuid();

        [MaxLength(200)]
        public string Name { get; set; } = default!;

        public int? ParentHotelId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
