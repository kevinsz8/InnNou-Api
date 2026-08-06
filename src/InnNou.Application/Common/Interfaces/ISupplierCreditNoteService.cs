using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISupplierCreditNoteService
    {
        Task<SupplierCreditNoteDto?> CreateAsync(Guid supplierReturnToken, string creditNoteNumber, DateTime creditNoteDate, string reason, string? notes, List<CreateSupplierCreditNoteLineInputDto> lines, IRequestContext context, CancellationToken cancellationToken = default);
        Task<SupplierCreditNoteDto?> GetByTokenAsync(Guid supplierCreditNoteToken, IRequestContext context, CancellationToken cancellationToken = default);
        Task<SupplierCreditNoteDto?> GetBySupplierReturnTokenAsync(Guid supplierReturnToken, IRequestContext context, CancellationToken cancellationToken = default);
        Task<PagedResult<SupplierCreditNoteDto>> GetPagedAsync(Guid? organizationToken, Guid? supplierToken, DateTime? fromDate, DateTime? toDate, string? purchaseOrderNumber, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
