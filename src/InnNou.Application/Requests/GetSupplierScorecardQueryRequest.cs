using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetSupplierScorecardQueryRequest : IRequest<ApiResponse<GetSupplierScorecardQueryResponse>>
    {
        public Guid SupplierToken { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
