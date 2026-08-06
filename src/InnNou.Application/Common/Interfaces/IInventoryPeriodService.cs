using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    // Inventory Periods — state-machine counting periods (OPEN -> IN_PROGRESS -> PRE_CLOSED ->
    // CLOSED) layered on top of Inventory's StockLevels/InventoryMovements, closing the reporting
    // gap of having no reconciliation checkpoint. OPEN/IN_PROGRESS/PRE_CLOSED are auto-computed
    // from count completeness (SubmitCountAsync); CLOSED only happens via the explicit CloseAsync
    // confirm action, which is when variance-driven ADJUSTMENT movements actually get posted.
    // Own service (not folded into IInventoryService), same precedent as IParLevelService being a
    // sibling service under the Inventory umbrella. See .claude/InventoryPeriodsModule.md.
    public interface IInventoryPeriodService
    {
        Task<InventoryPeriodDto?> OpenAsync(Guid warehouseToken, string? notes, IRequestContext context, CancellationToken cancellationToken);
        Task<InventoryPeriodDto?> SubmitCountAsync(Guid periodToken, Guid articleToken, decimal countedQuantity, Guid? unitToken, IRequestContext context, CancellationToken cancellationToken);
        Task<InventoryPeriodDto?> CloseAsync(Guid periodToken, IRequestContext context, CancellationToken cancellationToken);
        Task<InventoryPeriodDto?> ReopenAsync(Guid periodToken, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<InventoryPeriodDto>> GetPagedAsync(Guid? warehouseToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<InventoryPeriodDto?> GetByTokenAsync(Guid periodToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
