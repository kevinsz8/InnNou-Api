using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInventoryMovementsQueryHandler(IInventoryService inventoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetInventoryMovementsQueryRequest, ApiResponse<GetInventoryMovementsQueryResponse>>
    {
        public async Task<ApiResponse<GetInventoryMovementsQueryResponse>> Handle(GetInventoryMovementsQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<GetInventoryMovementsQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            var result = await inventoryService.GetMovementsAsync(
                request.WarehouseToken, request.ArticleToken, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetInventoryMovementsQueryResponse
            {
                Movements = mapper.MapList<Responses.Common.InventoryMovement>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetInventoryMovementsQueryResponse>.SuccessResponse(response);
        }
    }
}
