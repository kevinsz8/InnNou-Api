using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    // Requisitions ("Requisiciones internas") — a Department pulling stock from a Warehouse
    // (store) for internal use, the first "stock going out for an operational reason, not a
    // sale" flow in InnNou. Deliberately a separate domain (own tables, own service) rather than
    // folding into IInventoryService — it has its own approval+issuance lifecycle, closer in
    // shape to Internal Orders than to a plain Adjustment/Transfer. See CLAUDE.md's
    // "Requisitions" section.
    public interface IRequisitionService
    {
        Task<RequisitionDto?> CreateAsync(Guid warehouseToken, Guid departmentToken, string? notes, List<CreateRequisitionLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken);
        Task<RequisitionLineDto?> AddLineAsync(Guid requisitionToken, Guid articleToken, decimal quantityRequested, Guid? unitToken, string? notes, IRequestContext context, CancellationToken cancellationToken);
        Task<RequisitionLineDto?> EditLineAsync(Guid requisitionLineToken, decimal quantityRequested, Guid? unitToken, string? notes, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteLineAsync(Guid requisitionLineToken, IRequestContext context, CancellationToken cancellationToken);

        Task<RequisitionDto?> GetByTokenAsync(Guid requisitionToken, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<RequisitionDto>> GetPagedAsync(Guid? organizationToken, Guid? warehouseToken, Guid? departmentToken, string? status, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);

        Task<RequisitionDto?> ApproveAsync(Guid requisitionToken, IRequestContext context, CancellationToken cancellationToken);
        Task<RequisitionDto?> RejectAsync(Guid requisitionToken, string reason, IRequestContext context, CancellationToken cancellationToken);
        Task<RequisitionDto?> CancelAsync(Guid requisitionToken, string? reason, IRequestContext context, CancellationToken cancellationToken);
        Task<RequisitionDto?> CloseShortAsync(Guid requisitionToken, string reason, IRequestContext context, CancellationToken cancellationToken);

        Task<RequisitionIssueDto?> CreateIssueAsync(Guid requisitionToken, string? notes, List<CreateRequisitionIssueLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken);
    }
}
