namespace InnNou.Domain.Dtos
{
    public class HotelDto
    {
        public int HotelId { get; set; }

        public Guid HotelToken { get; set; }

        public string Name { get; set; }

        public int? ParentHotelId { get; set; }

        public bool IsActive { get; set; }
    }
}
