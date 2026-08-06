using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierCreditNotesQueryRequest : IRequest<ApiResponse<GetSupplierCreditNotesQueryResponse>>
    {
        public Guid? OrganizationToken { get; set; }
        public Guid? SupplierToken { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
