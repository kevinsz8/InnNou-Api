using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    // Inventory ("existencias") — on-hand stock quantity per (Warehouse, Article), backed by an
    // append-only movement ledger. Record-only in V1: RECEIPT (automatic, from Goods Receipts),
    // ADJUSTMENT (manual, physical-count correction), TRANSFER (warehouse-to-warehouse, same
    // Organization only). No consumption/production/sale movements yet, no cost/valuation
    // tracking. Unlike Goods Receipts (folded into IPurchaseOrderService), Inventory is its own
    // orthogonal domain and gets its own service. See .claude/InventoryModule.md.
    public interface IInventoryService
    {
        Task<StockLevelDto?> CreateAdjustmentAsync(Guid warehouseToken, Guid articleToken, decimal deltaQuantity, string reason, IRequestContext context, CancellationToken cancellationToken);
        Task<InventoryTransferDto?> CreateTransferAsync(Guid fromWarehouseToken, Guid toWarehouseToken, string? notes, List<CreateInventoryTransferLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<StockLevelDto>> GetStockLevelsAsync(Guid? warehouseToken, Guid? articleToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<InventoryMovementDto>> GetMovementsAsync(Guid warehouseToken, Guid? articleToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<InventoryTransferDto>> GetTransfersAsync(Guid? warehouseToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<InventoryTransferDto?> GetTransferByTokenAsync(Guid transferToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
