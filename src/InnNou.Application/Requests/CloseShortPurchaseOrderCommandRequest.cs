using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CloseShortPurchaseOrderCommandRequest : IRequest<ApiResponse<CloseShortPurchaseOrderCommandResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
