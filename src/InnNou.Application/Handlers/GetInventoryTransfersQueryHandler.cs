using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInventoryTransfersQueryHandler(IInventoryService inventoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetInventoryTransfersQueryRequest, ApiResponse<GetInventoryTransfersQueryResponse>>
    {
        public async Task<ApiResponse<GetInventoryTransfersQueryResponse>> Handle(GetInventoryTransfersQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await inventoryService.GetTransfersAsync(
                request.WarehouseToken, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetInventoryTransfersQueryResponse
            {
                InventoryTransfers = mapper.MapList<Responses.Common.InventoryTransfer>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetInventoryTransfersQueryResponse>.SuccessResponse(response);
        }
    }
}
