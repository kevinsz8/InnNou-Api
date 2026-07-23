using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetConsolidatedPurchaseOrdersQueryHandler(IConsolidatedPurchaseOrderService consolidatedPurchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetConsolidatedPurchaseOrdersQueryRequest, ApiResponse<GetConsolidatedPurchaseOrdersQueryResponse>>
    {
        public async Task<ApiResponse<GetConsolidatedPurchaseOrdersQueryResponse>> Handle(GetConsolidatedPurchaseOrdersQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await consolidatedPurchaseOrderService.GetPagedAsync(
                request.SuperAssociateOrganizationToken, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            return ApiResponse<GetConsolidatedPurchaseOrdersQueryResponse>.SuccessResponse(new GetConsolidatedPurchaseOrdersQueryResponse
            {
                ConsolidatedPurchaseOrders = mapper.MapList<Responses.Common.ConsolidatedPurchaseOrder>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            });
        }
    }
}
