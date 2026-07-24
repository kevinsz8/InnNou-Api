using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInventoryTransferByTokenQueryHandler(IInventoryService inventoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetInventoryTransferByTokenQueryRequest, ApiResponse<GetInventoryTransferByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetInventoryTransferByTokenQueryResponse>> Handle(GetInventoryTransferByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.InventoryTransferToken == Guid.Empty)
                return ApiResponse<GetInventoryTransferByTokenQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "InventoryTransferToken is required.", 400);

            var result = await inventoryService.GetTransferByTokenAsync(request.InventoryTransferToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetInventoryTransferByTokenQueryResponse>.FailureResponse(ErrorCodes.InventoryTransferNotFound, "Inventory transfer not found.", 404);

            return ApiResponse<GetInventoryTransferByTokenQueryResponse>.SuccessResponse(new GetInventoryTransferByTokenQueryResponse
            {
                InventoryTransfer = mapper.Map<Responses.Common.InventoryTransfer>(result)
            });
        }
    }
}
