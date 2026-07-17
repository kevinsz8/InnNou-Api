using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetPurchaseOrderByTokenQueryHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetPurchaseOrderByTokenQueryRequest, ApiResponse<GetPurchaseOrderByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetPurchaseOrderByTokenQueryResponse>> Handle(GetPurchaseOrderByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var purchaseOrder = await purchaseOrderService.GetByTokenAsync(request.PurchaseOrderToken, context, cancellationToken);
            if (purchaseOrder is null)
                return ApiResponse<GetPurchaseOrderByTokenQueryResponse>.FailureResponse(ErrorCodes.PurchaseOrderNotFound, "Purchase order not found.", 404);

            return ApiResponse<GetPurchaseOrderByTokenQueryResponse>.SuccessResponse(new GetPurchaseOrderByTokenQueryResponse
            {
                PurchaseOrder = mapper.Map<Responses.Common.PurchaseOrder>(purchaseOrder)
            });
        }
    }
}
