using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetStockLevelsQueryHandler(IInventoryService inventoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetStockLevelsQueryRequest, ApiResponse<GetStockLevelsQueryResponse>>
    {
        public async Task<ApiResponse<GetStockLevelsQueryResponse>> Handle(GetStockLevelsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await inventoryService.GetStockLevelsAsync(
                request.WarehouseToken, request.ArticleToken, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetStockLevelsQueryResponse
            {
                StockLevels = mapper.MapList<Responses.Common.StockLevel>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetStockLevelsQueryResponse>.SuccessResponse(response);
        }
    }
}
