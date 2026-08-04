namespace InnNou.Application.Responses.Common
{
    public class Notification
    {
        public Guid NotificationToken { get; set; }
        public string Type { get; set; } = default!;
        public string DataJson { get; set; } = default!;
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
