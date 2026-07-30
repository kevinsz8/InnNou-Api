using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetEligibleReturnLinesQueryRequest : IRequest<ApiResponse<GetEligibleReturnLinesQueryResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
    }
}
