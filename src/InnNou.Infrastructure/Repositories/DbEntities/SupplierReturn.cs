using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierReturn
    {
        public int SupplierReturnId { get; set; }
        public Guid SupplierReturnToken { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;

        // Only populated by sp_SupplierReturn_GetByToken/GetPaged (joins through PurchaseOrder) —
        // sp_SupplierReturn_Create/Close leave these at their default and the service overwrites
        // from the already-known PurchaseOrder instead, same convention as PurchaseOrderLine's
        // own "not every SP populates every denormalized field" fields.
        public int OrganizationId { get; set; }
        public int WarehouseId { get; set; }
        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string? SupplierName { get; set; }

        public SupplierReturnStatus Status { get; set; }
        public SupplierReturnResolutionType? ResolutionType { get; set; }
        public string? Notes { get; set; }
        public DateTime? ClosedUtc { get; set; }
        public string? ClosedBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Only populated by sp_SupplierReturn_GetPaged (a cheap CROSS APPLY COUNT).
        public int LineCount { get; set; }
    }
}
