using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierInvoiceMatchToleranceQueryRequest : IRequest<ApiResponse<GetSupplierInvoiceMatchToleranceQueryResponse>>
    {
        public Guid OrganizationToken { get; set; }
    }
}
