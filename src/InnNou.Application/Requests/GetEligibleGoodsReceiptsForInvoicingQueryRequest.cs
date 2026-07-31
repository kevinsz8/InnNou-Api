using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetEligibleGoodsReceiptsForInvoicingQueryRequest : IRequest<ApiResponse<GetEligibleGoodsReceiptsForInvoicingQueryResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public Guid SupplierToken { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string? DeliveryNoteNumber { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        // "ORDER_DATE" (default) or "RECEIPT_DATE" — which date FromDate/ToDate filter against.
        public string? DateType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
