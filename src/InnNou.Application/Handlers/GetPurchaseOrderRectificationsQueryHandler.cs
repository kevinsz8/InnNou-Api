using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetPurchaseOrderRectificationsQueryHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetPurchaseOrderRectificationsQueryRequest, ApiResponse<GetPurchaseOrderRectificationsQueryResponse>>
    {
        public async Task<ApiResponse<GetPurchaseOrderRectificationsQueryResponse>> Handle(GetPurchaseOrderRectificationsQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.PurchaseOrderToken == Guid.Empty)
                return ApiResponse<GetPurchaseOrderRectificationsQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "PurchaseOrderToken is required.", 400);

            var result = await purchaseOrderService.GetRectificationsAsync(request.PurchaseOrderToken, context, cancellationToken);

            return ApiResponse<GetPurchaseOrderRectificationsQueryResponse>.SuccessResponse(new GetPurchaseOrderRectificationsQueryResponse
            {
                Rectifications = mapper.MapList<Responses.Common.PurchaseOrderRectification>(result)
            });
        }
    }
}
