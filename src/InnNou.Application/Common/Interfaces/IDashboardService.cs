using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    // Dashboard ("Home") summary — a deliberately isolated, read-only module: its own service,
    // its own stored procedures, no reuse of/changes to any existing service or SP even where one
    // is close to what's needed. Confirmed with the user: these are pure reads, so duplicating a
    // bit of query logic carries none of the risk duplicating write logic would, and it keeps this
    // module from ever being able to destabilize an existing flow as it evolves independently.
    // See .claude/DashboardModule.md.
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(IRequestContext context, CancellationToken cancellationToken);
    }
}
