namespace InnNou.Application.Responses.Common
{
    public class RecentActivityItem
    {
        public string ActivityType { get; set; } = default!;
        public string? ReferenceLabel { get; set; }
        public string? ActorName { get; set; }
        public DateTime OccurredUtc { get; set; }
    }
}
