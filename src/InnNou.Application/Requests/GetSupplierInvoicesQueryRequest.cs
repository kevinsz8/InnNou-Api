using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierInvoicesQueryRequest : IRequest<ApiResponse<GetSupplierInvoicesQueryResponse>>
    {
        public Guid? OrganizationToken { get; set; }
        public Guid? SupplierToken { get; set; }
        public string? Status { get; set; }
        public string? SearchText { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
