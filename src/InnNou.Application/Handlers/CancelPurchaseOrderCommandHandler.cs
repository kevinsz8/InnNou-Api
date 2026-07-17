using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CancelPurchaseOrderCommandHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CancelPurchaseOrderCommandRequest, ApiResponse<CancelPurchaseOrderCommandResponse>>
    {
        public async Task<ApiResponse<CancelPurchaseOrderCommandResponse>> Handle(CancelPurchaseOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.PurchaseOrderToken == Guid.Empty)
                return ApiResponse<CancelPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "PurchaseOrderToken is required.", 400);

            var purchaseOrder = await purchaseOrderService.CancelAsync(request.PurchaseOrderToken, context, cancellationToken);
            if (purchaseOrder is null)
                return ApiResponse<CancelPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.PurchaseOrderNotFound, "Purchase order not found.", 404);

            return ApiResponse<CancelPurchaseOrderCommandResponse>.SuccessResponse(new CancelPurchaseOrderCommandResponse
            {
                PurchaseOrder = mapper.Map<Responses.Common.PurchaseOrder>(purchaseOrder)
            });
        }
    }
}
