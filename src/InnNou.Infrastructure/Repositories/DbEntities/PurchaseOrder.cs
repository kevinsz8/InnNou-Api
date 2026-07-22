using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class PurchaseOrder
    {
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }

        // Internal-use only, resolved by the extended sp_PurchaseOrder_Create so
        // OrderService.CompleteSubmissionAsync can send a per-supplier confirmation email
        // without a second query per group. Deliberately never mapped into PurchaseOrderDto —
        // same "transient field that never round-trips a response" convention as
        // ArticlePriceDto's transient SupplierId.
        public string? SupplierEmail { get; set; }

        // Same internal-use-only convention as SupplierEmail above — drives the "New purchase
        // order" email/PDF's language (OrderConfirmationLocalization falls back to "en" when null).
        public string? SupplierLanguageCode { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public PurchaseOrderStatus Status { get; set; }
        public DateTime SentUtc { get; set; }
        public DateTime? CancelledUtc { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Only populated by sp_PurchaseOrder_GetPaged (a cheap CROSS APPLY COUNT, not per-row
        // app-level N+1); GetByToken/Cancel leave this at 0 and PurchaseOrderService overwrites
        // it from the real hydrated Lines.Count instead.
        public int LineCount { get; set; }
    }
}
