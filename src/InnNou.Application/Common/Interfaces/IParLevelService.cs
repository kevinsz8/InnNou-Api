using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    // Par Levels ("niveles de par") — a minimum stock threshold + reorder quantity per
    // (Warehouse, Article). When on-hand stock drops below the effective minimum it surfaces on
    // a "below par" list for a human to act on — never auto-creates an Order, same "suggest,
    // don't auto-execute" philosophy as Order Templates. A base level always exists as the
    // fallback; a SEASONAL override (a recurring month/day window, e.g. "temporada alta") or an
    // EVENT override (a one-off literal date range, e.g. a confirmed wedding) can refine it —
    // priority EVENT > SEASONAL > BASE. LeadTimeDays is surfaced as-is alongside the below-par
    // list, never turned into a computed urgency score (no consumption-rate data exists to back
    // that up). See .claude/ParLevelsModule.md.
    public interface IParLevelService
    {
        Task<ParLevelDto?> CreateBaseAsync(Guid warehouseToken, Guid articleToken, decimal minimumQuantity, decimal reorderQuantity, IRequestContext context, CancellationToken cancellationToken);
        Task<ParLevelDto?> EditBaseAsync(Guid parLevelToken, decimal minimumQuantity, decimal reorderQuantity, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteBaseAsync(Guid parLevelToken, IRequestContext context, CancellationToken cancellationToken);

        Task<ParLevelOverrideDto?> CreateOverrideAsync(
            Guid warehouseToken, Guid articleToken, ParLevelOverrideType type, string? label,
            decimal minimumQuantity, decimal reorderQuantity,
            int? startMonth, int? startDay, int? endMonth, int? endDay,
            DateOnly? startDate, DateOnly? endDate,
            IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteOverrideAsync(Guid parLevelOverrideToken, IRequestContext context, CancellationToken cancellationToken);

        Task<ParLevelConfigurationDto?> GetConfigurationAsync(Guid warehouseToken, Guid articleToken, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<BelowParRowDto>> GetBelowParAsync(Guid? warehouseToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
    }
}
