namespace InnNou.Application.Responses.Common
{
    public class Hotel
    {
        public int HotelId { get; set; }
        public Guid HotelToken { get; set; }

        public string Name { get; set; }

        public int? ParentHotelId { get; set; }

        public bool IsActive { get; set; }
    }
}
