namespace InnNou.Domain.Dtos
{
    public class HotelContactDto
    {
        public int HotelContactId { get; set; }
        public Guid HotelContactToken { get; set; }
        public Guid HotelToken { get; set; }
        public int HotelId { get; set; }
        public string ContactName { get; set; } = default!;
        public string? ContactType { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
