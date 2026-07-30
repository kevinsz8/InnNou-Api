using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISupplierReturnService
    {
        // Rejected GoodsReceiptLines for this PurchaseOrder not yet claimed by any
        // SupplierReturn — feeds the "new return" line picker. Null means the PurchaseOrder
        // wasn't found or isn't visible to the caller.
        Task<List<EligibleReturnLineDto>?> GetEligibleLinesAsync(Guid purchaseOrderToken, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierReturnDto?> CreateAsync(Guid purchaseOrderToken, string? notes, List<Guid> goodsReceiptLineTokens, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierReturnDto?> CloseAsync(Guid supplierReturnToken, string resolutionType, string? notes, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierReturnDto?> GetByTokenAsync(Guid supplierReturnToken, IRequestContext context, CancellationToken cancellationToken);

        Task<PagedResult<SupplierReturnDto>> GetPagedAsync(Guid? organizationToken, Guid? supplierToken, string? status, DateTime? fromDate, DateTime? toDate, string? purchaseOrderNumber, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
    }
}
