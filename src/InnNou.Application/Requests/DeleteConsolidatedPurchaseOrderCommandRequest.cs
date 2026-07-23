using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteConsolidatedPurchaseOrderCommandRequest : IRequest<ApiResponse<DeleteConsolidatedPurchaseOrderCommandResponse>>
    {
        public Guid ConsolidatedPurchaseOrderToken { get; set; }
    }
}
