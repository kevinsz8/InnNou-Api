using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISupplierInvoiceService
    {
        Task<PagedResult<SupplierInvoiceDto>> GetPagedAsync(Guid? organizationToken, Guid? supplierToken, string? status, string? searchText, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierInvoiceDto?> GetByTokenAsync(Guid supplierInvoiceToken, IRequestContext context, CancellationToken cancellationToken);

        Task<PagedResult<GoodsReceiptForInvoicingDto>> GetEligibleGoodsReceiptsForInvoicingAsync(Guid organizationToken, Guid supplierToken, string? purchaseOrderNumber, string? deliveryNoteNumber, DateTime? fromDate, DateTime? toDate, string? dateType, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierInvoiceDto?> CreateAsync(Guid organizationToken, Guid supplierToken, string supplierInvoiceNumber, DateTime invoiceDate, string? notes, List<Guid> goodsReceiptTokens, List<CreateSupplierInvoiceLineInputDto> lines, List<CreateSupplierInvoiceTaxBreakdownInputDto> taxBreakdown, IRequestContext context, CancellationToken cancellationToken);

        Task<bool> UploadAttachmentAsync(Guid supplierInvoiceToken, Stream fileStream, string fileExtension, IRequestContext context, CancellationToken cancellationToken);

        Task<(byte[] Bytes, string Extension)?> DownloadAttachmentAsync(Guid supplierInvoiceToken, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierInvoiceMatchToleranceDto?> GetEffectiveToleranceAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierInvoiceMatchToleranceDto?> UpsertToleranceAsync(Guid organizationToken, decimal tolerancePercent, decimal toleranceAmount, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierInvoicePurchaseOrderPolicyDto?> GetEffectivePurchaseOrderPolicyAsync(Guid organizationToken, IRequestContext context, CancellationToken cancellationToken);

        Task<SupplierInvoicePurchaseOrderPolicyDto?> UpsertPurchaseOrderPolicyAsync(Guid organizationToken, bool allowMultiplePurchaseOrders, IRequestContext context, CancellationToken cancellationToken);
    }
}
