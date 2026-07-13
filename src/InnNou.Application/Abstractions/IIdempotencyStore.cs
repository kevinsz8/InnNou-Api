namespace InnNou.Application.Abstractions
{
    public enum IdempotencyOutcome
    {
        Inserted,
        Pending,
        Completed,
        HashMismatch
    }

    public class IdempotencyBeginResult
    {
        public IdempotencyOutcome Outcome { get; set; }
        public int? ResponseStatusCode { get; set; }
        public string? ResponseBody { get; set; }
    }

    public interface IIdempotencyStore
    {
        Task<IdempotencyBeginResult> TryBeginAsync(
            string key, string requestType, Guid userToken, string requestHash,
            DateTime createdUtc, DateTime expiresUtc, CancellationToken cancellationToken);

        Task CompleteAsync(
            string key, string requestType, Guid userToken,
            int? responseStatusCode, string responseBody, DateTime completedUtc, CancellationToken cancellationToken);

        Task ReleaseAsync(string key, string requestType, Guid userToken, CancellationToken cancellationToken);
    }
}
