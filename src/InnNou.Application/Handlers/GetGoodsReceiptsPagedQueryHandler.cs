using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetGoodsReceiptsPagedQueryHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetGoodsReceiptsPagedQueryRequest, ApiResponse<GetGoodsReceiptsPagedQueryResponse>>
    {
        public async Task<ApiResponse<GetGoodsReceiptsPagedQueryResponse>> Handle(GetGoodsReceiptsPagedQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await purchaseOrderService.GetGoodsReceiptsPagedAsync(
                request.OrganizationToken, request.WarehouseToken, request.PurchaseOrderNumber, request.DeliveryNoteNumber,
                request.FromDate, request.ToDate, request.PageNumber, request.PageSize, context, cancellationToken);

            var response = new GetGoodsReceiptsPagedQueryResponse
            {
                GoodsReceipts = mapper.MapList<Responses.Common.GoodsReceiptSummary>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                HasNextPage = result.PageNumber < result.TotalPages,
                HasPreviousPage = result.PageNumber > 1
            };
            return ApiResponse<GetGoodsReceiptsPagedQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
