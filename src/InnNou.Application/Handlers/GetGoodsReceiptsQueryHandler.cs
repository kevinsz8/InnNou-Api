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
