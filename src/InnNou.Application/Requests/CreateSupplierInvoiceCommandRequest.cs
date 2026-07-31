using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateSupplierInvoiceLineRequestItem
    {
        public Guid GoodsReceiptLineToken { get; set; }
        public decimal QuantityInvoiced { get; set; }
        public decimal UnitPriceInvoiced { get; set; }
    }

    // "Base Fra" per tax rate, typed by the caller from the supplier's real invoice — see
    // CreateSupplierInvoiceTaxBreakdownInputDto for the full rationale.
    public class CreateSupplierInvoiceTaxBreakdownRequestItem
    {
        public decimal? TaxRatePercent { get; set; }
        public decimal BaseAmount { get; set; }
    }

    public class CreateSupplierInvoiceCommandRequest : IRequest<ApiResponse<CreateSupplierInvoiceCommandResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public Guid SupplierToken { get; set; }
        public string SupplierInvoiceNumber { get; set; } = default!;
        public DateTime InvoiceDate { get; set; }
        public string? Notes { get; set; }
        public List<Guid> GoodsReceiptTokens { get; set; } = [];
        public List<CreateSupplierInvoiceLineRequestItem> Lines { get; set; } = [];
        public List<CreateSupplierInvoiceTaxBreakdownRequestItem> TaxBreakdown { get; set; } = [];
    }
}
