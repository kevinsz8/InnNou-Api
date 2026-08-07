using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetGoodsReceiptsQueryHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetGoodsReceiptsQueryRequest, ApiResponse<GetGoodsReceiptsQueryResponse>>
    {
        public async Task<ApiResponse<GetGoodsReceiptsQueryResponse>> Handle(GetGoodsReceiptsQueryRequest request, CancellationToken cancellationToken)
        {
            // This list is always meant to be scoped to a handful of receipts for one
            // PurchaseOrder (see PurchaseOrderService.GetGoodsReceiptsAsync's own comment) — the
            // per-row Lines hydration it does is only safe under that assumption. Enforce it here
            // instead of silently falling through to an unbounded org/supplier-wide browse.
            if (!request.PurchaseOrderToken.HasValue)
                return ApiResponse<GetGoodsReceiptsQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "PurchaseOrderToken is required.", 400);

            var result = await purchaseOrderService.GetGoodsReceiptsAsync(
                request.PurchaseOrderToken, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetGoodsReceiptsQueryResponse
            {
                GoodsReceipts = mapper.MapList<Responses.Common.GoodsReceipt>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetGoodsReceiptsQueryResponse>.SuccessResponse(response);
        }
    }
}
