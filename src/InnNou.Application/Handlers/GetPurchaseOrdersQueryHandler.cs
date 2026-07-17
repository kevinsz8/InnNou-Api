using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetPurchaseOrdersQueryHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetPurchaseOrdersQueryRequest, ApiResponse<GetPurchaseOrdersQueryResponse>>
    {
        public async Task<ApiResponse<GetPurchaseOrdersQueryResponse>> Handle(GetPurchaseOrdersQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await purchaseOrderService.GetPagedAsync(
                request.OrganizationToken, request.Status, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetPurchaseOrdersQueryResponse
            {
                PurchaseOrders = mapper.MapList<Responses.Common.PurchaseOrder>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetPurchaseOrdersQueryResponse>.SuccessResponse(response);
        }
    }
}
