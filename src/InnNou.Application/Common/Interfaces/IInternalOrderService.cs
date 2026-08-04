using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    // Internal Orders ("Pedidos Internos") — moves stock BETWEEN two different Asociado
    // Organizations under the same Super Asociado (each its own legal entity), distinct from
    // Inventory Transfers (IInventoryService.CreateTransferAsync, same-Organization only). A
    // separate domain deliberately kept out of Order/PurchaseOrder/SupplierInvoice and Inventory
    // — own tables, own service, own frontend pages. See CLAUDE.md's "Internal Orders" section.
    public interface IInternalOrderService
    {
        Task<InternalOrderDto?> CreateAsync(Guid sourceOrganizationToken, Guid destinationWarehouseToken, string? notes, List<CreateInternalOrderLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken);
        Task<InternalOrderDto?> GetByTokenAsync(Guid internalOrderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<InternalOrderDto>> GetPagedAsync(string? direction, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<InternalOrderDto?> CancelAsync(Guid internalOrderToken, string reason, IRequestContext context, CancellationToken cancellationToken);
        Task<InternalOrderShipmentDto?> CreateShipmentAsync(Guid internalOrderToken, Guid sourceWarehouseToken, string? notes, List<CreateInternalOrderShipmentLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken);
        Task<InternalOrderReceiptDto?> CreateReceiptAsync(Guid internalOrderToken, string? notes, List<CreateInternalOrderReceiptLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken);

        // Every other ASSOCIATE organization under the caller's own nearest Super Asociado — the
        // real set CreateAsync's own same-Super-Asociado check enforces, surfaced here purely so
        // the frontend's "source organization" picker can offer valid choices instead of relying
        // on a create-time rejection. See sp_Organization_GetPeerAssociates.
        Task<List<OrganizationDto>> GetEligibleSourceOrganizationsAsync(IRequestContext context, CancellationToken cancellationToken);
    }
}
