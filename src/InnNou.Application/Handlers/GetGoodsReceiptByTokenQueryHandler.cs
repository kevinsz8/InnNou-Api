using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetGoodsReceiptByTokenQueryHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetGoodsReceiptByTokenQueryRequest, ApiResponse<GetGoodsReceiptByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetGoodsReceiptByTokenQueryResponse>> Handle(GetGoodsReceiptByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.GoodsReceiptToken == Guid.Empty)
                return ApiResponse<GetGoodsReceiptByTokenQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "GoodsReceiptToken is required.", 400);

            var result = await purchaseOrderService.GetGoodsReceiptByTokenAsync(request.GoodsReceiptToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetGoodsReceiptByTokenQueryResponse>.FailureResponse(ErrorCodes.GoodsReceiptNotFound, "Goods receipt not found.", 404);

            return ApiResponse<GetGoodsReceiptByTokenQueryResponse>.SuccessResponse(new GetGoodsReceiptByTokenQueryResponse
            {
                GoodsReceipt = mapper.Map<Responses.Common.GoodsReceipt>(result)
            });
        }
    }
}
