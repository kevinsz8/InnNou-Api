namespace InnNou.Application.Responses.Common
{
    public class User
    {
        public int UserId { get; set; }
        public Guid UserToken { get; set; }
        public string Email { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public int? HotelId { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}
