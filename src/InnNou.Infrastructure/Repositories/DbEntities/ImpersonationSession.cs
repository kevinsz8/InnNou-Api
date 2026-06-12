namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class ImpersonationSession
    {
        public int ImpersonationSessionId { get; set; }
        public Guid ImpersonationToken { get; set; }
        public int ActorUserId { get; set; }
        public int TargetUserId { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime? EndedUtc { get; set; }
        public string? Reason { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
