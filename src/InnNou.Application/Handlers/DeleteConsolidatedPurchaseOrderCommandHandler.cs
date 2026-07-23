using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteConsolidatedPurchaseOrderCommandHandler(IConsolidatedPurchaseOrderService consolidatedPurchaseOrderService, IRequestContext context)
        : IRequestHandler<DeleteConsolidatedPurchaseOrderCommandRequest, ApiResponse<DeleteConsolidatedPurchaseOrderCommandResponse>>
    {
        public async Task<ApiResponse<DeleteConsolidatedPurchaseOrderCommandResponse>> Handle(DeleteConsolidatedPurchaseOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.ConsolidatedPurchaseOrderToken == Guid.Empty)
                return ApiResponse<DeleteConsolidatedPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ConsolidatedPurchaseOrderToken is required.", 400);

            var deleted = await consolidatedPurchaseOrderService.DeleteAsync(request.ConsolidatedPurchaseOrderToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteConsolidatedPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.ConsolidatedPurchaseOrderNotFound, "Consolidated purchase order not found.", 404);

            return ApiResponse<DeleteConsolidatedPurchaseOrderCommandResponse>.SuccessResponse(new DeleteConsolidatedPurchaseOrderCommandResponse
            {
                ConsolidatedPurchaseOrderToken = request.ConsolidatedPurchaseOrderToken,
                Success = true
            });
        }
    }
}
