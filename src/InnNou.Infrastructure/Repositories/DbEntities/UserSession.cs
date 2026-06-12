namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class UserSession
    {
        public int UserSessionId { get; set; }
        public Guid SessionToken { get; set; }
        public int UserId { get; set; }
        public DateTime LoginUtc { get; set; }
        public DateTime? LogoutUtc { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceName { get; set; }
        public bool IsActive { get; set; }
    }
}
