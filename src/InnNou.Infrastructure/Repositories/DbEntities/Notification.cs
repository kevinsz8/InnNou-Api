using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public Guid NotificationToken { get; set; }
        public int UserId { get; set; }
        public NotificationType Type { get; set; }

        // Raw interpolation params for the frontend's i18next template — never resolved to a
        // fixed-language sentence server-side.
        public string DataJson { get; set; } = default!;
        public string? LinkUrl { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadUtc { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
