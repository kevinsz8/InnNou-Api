using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CloseShortPurchaseOrderCommandHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CloseShortPurchaseOrderCommandRequest, ApiResponse<CloseShortPurchaseOrderCommandResponse>>
    {
        public async Task<ApiResponse<CloseShortPurchaseOrderCommandResponse>> Handle(CloseShortPurchaseOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.PurchaseOrderToken == Guid.Empty)
                return ApiResponse<CloseShortPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "PurchaseOrderToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<CloseShortPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.PurchaseOrderCloseShortReasonRequired, "A reason is required to close a purchase order as short.", 400);

            var purchaseOrder = await purchaseOrderService.CloseShortAsync(request.PurchaseOrderToken, request.Reason, context, cancellationToken);
            if (purchaseOrder is null)
                return ApiResponse<CloseShortPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.PurchaseOrderNotFound, "Purchase order not found.", 404);

            return ApiResponse<CloseShortPurchaseOrderCommandResponse>.SuccessResponse(new CloseShortPurchaseOrderCommandResponse
            {
                PurchaseOrder = mapper.Map<Responses.Common.PurchaseOrder>(purchaseOrder)
            });
        }
    }
}
