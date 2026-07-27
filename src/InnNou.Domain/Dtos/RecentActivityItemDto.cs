namespace InnNou.Domain.Dtos
{
    public class RecentActivityItemDto
    {
        public string ActivityType { get; set; } = default!;
        public string? ReferenceLabel { get; set; }
        public string? ActorName { get; set; }
        public DateTime OccurredUtc { get; set; }
    }
}
