using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetGoodsReceiptTaxPreviewQueryHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetGoodsReceiptTaxPreviewQueryRequest, ApiResponse<GetGoodsReceiptTaxPreviewQueryResponse>>
    {
        public async Task<ApiResponse<GetGoodsReceiptTaxPreviewQueryResponse>> Handle(GetGoodsReceiptTaxPreviewQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await purchaseOrderService.GetGoodsReceiptTaxPreviewAsync(request.PurchaseOrderToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetGoodsReceiptTaxPreviewQueryResponse>.FailureResponse(ErrorCodes.PurchaseOrderNotFound, "Purchase order not found.", 404);

            return ApiResponse<GetGoodsReceiptTaxPreviewQueryResponse>.SuccessResponse(new GetGoodsReceiptTaxPreviewQueryResponse
            {
                Lines = mapper.MapList<Responses.Common.GoodsReceiptTaxPreviewLine>(result)
            });
        }
    }
}
