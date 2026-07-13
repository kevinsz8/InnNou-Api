using Dapper;
using InnNou.Application.Abstractions;
using InnNou.Infrastructure.Abstractions;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class IdempotencyStore(IDbConnectionFactory connectionFactory) : IIdempotencyStore
{
    private sealed class TryBeginRow
    {
        public string Outcome { get; set; } = default!;
        public int? ResponseStatusCode { get; set; }
        public string? ResponseBody { get; set; }
    }

    public async Task<IdempotencyBeginResult> TryBeginAsync(
        string key, string requestType, Guid userToken, string requestHash,
        DateTime createdUtc, DateTime expiresUtc, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var row = await connection.QuerySingleAsync<TryBeginRow>(
            "sp_IdempotencyKey_TryBegin",
            new
            {
                Key = key,
                RequestType = requestType,
                UserToken = userToken,
                RequestHash = requestHash,
                CreatedUtc = createdUtc,
                ExpiresUtc = expiresUtc
            },
            commandType: CommandType.StoredProcedure);

        return new IdempotencyBeginResult
        {
            Outcome = Enum.Parse<IdempotencyOutcome>(row.Outcome),
            ResponseStatusCode = row.ResponseStatusCode,
            ResponseBody = row.ResponseBody
        };
    }

    public async Task CompleteAsync(
        string key, string requestType, Guid userToken,
        int? responseStatusCode, string responseBody, DateTime completedUtc, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(
            "sp_IdempotencyKey_Complete",
            new
            {
                Key = key,
                RequestType = requestType,
                UserToken = userToken,
                ResponseStatusCode = responseStatusCode,
                ResponseBody = responseBody,
                CompletedUtc = completedUtc
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task ReleaseAsync(string key, string requestType, Guid userToken, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        await connection.ExecuteAsync(
            "sp_IdempotencyKey_Release",
            new { Key = key, RequestType = requestType, UserToken = userToken },
            commandType: CommandType.StoredProcedure);
    }
}
