using Dapper;
using InnNou.Infrastructure.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Threading;

namespace InnNou.Infrastructure.Services;

// Periodically purges expired IdempotencyKeys rows via sp_IdempotencyKey_Purge — nothing on
// the request path ever deletes rows itself, so without this they'd accumulate forever.
public class IdempotencyKeyCleanupService(
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration,
    ILogger<IdempotencyKeyCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = int.TryParse(configuration["Idempotency:CleanupIntervalHours"], out var configuredHours)
            ? configuredHours
            : 1;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var connection = connectionFactory.CreateConnection();

                await connection.ExecuteAsync(
                    "sp_IdempotencyKey_Purge",
                    new { BeforeUtc = DateTime.UtcNow },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                // Best-effort cleanup — a transient failure here should never crash the host;
                // it'll just retry on the next tick.
                logger.LogWarning(ex, "Failed to purge expired idempotency keys.");
            }
        }
    }
}
