using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetPurchaseOrderRectificationByTokenQueryHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetPurchaseOrderRectificationByTokenQueryRequest, ApiResponse<GetPurchaseOrderRectificationByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetPurchaseOrderRectificationByTokenQueryResponse>> Handle(GetPurchaseOrderRectificationByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.PurchaseOrderRectificationToken == Guid.Empty)
                return ApiResponse<GetPurchaseOrderRectificationByTokenQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "PurchaseOrderRectificationToken is required.", 400);

            var result = await purchaseOrderService.GetRectificationByTokenAsync(request.PurchaseOrderRectificationToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetPurchaseOrderRectificationByTokenQueryResponse>.FailureResponse(ErrorCodes.PurchaseOrderRectificationNotFound, "Purchase order rectification not found.", 404);

            return ApiResponse<GetPurchaseOrderRectificationByTokenQueryResponse>.SuccessResponse(new GetPurchaseOrderRectificationByTokenQueryResponse
            {
                PurchaseOrderRectification = mapper.Map<Responses.Common.PurchaseOrderRectification>(result)
            });
        }
    }
}
