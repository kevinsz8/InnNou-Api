using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateSupplierInvoiceLineRequestItem
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public decimal QuantityInvoiced { get; set; }
        public decimal UnitPriceInvoiced { get; set; }
    }

    public class CreateSupplierInvoiceCommandRequest : IRequest<ApiResponse<CreateSupplierInvoiceCommandResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public Guid SupplierToken { get; set; }
        public string SupplierInvoiceNumber { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public string? Notes { get; set; }
        public List<Guid> PurchaseOrderTokens { get; set; } = [];
        public List<CreateSupplierInvoiceLineRequestItem> Lines { get; set; } = [];
    }
}
