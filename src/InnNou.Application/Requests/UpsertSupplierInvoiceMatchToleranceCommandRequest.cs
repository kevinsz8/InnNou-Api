using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class UpsertSupplierInvoiceMatchToleranceCommandRequest : IRequest<ApiResponse<UpsertSupplierInvoiceMatchToleranceCommandResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public decimal TolerancePercent { get; set; }
        public decimal ToleranceAmount { get; set; }
    }
}
