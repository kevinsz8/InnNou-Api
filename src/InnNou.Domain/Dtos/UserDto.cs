namespace InnNou.Domain.Dtos
{
    public class UserDto
    {
        public int UserId { get; set; }
        public Guid UserToken { get; set; }
        public int RoleId { get; set; }
        public int? HotelId { get; set; }
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public bool IsActive { get; set; }
        
    }
}
