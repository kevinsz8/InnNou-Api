using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateInventoryAdjustmentCommandHandler(IInventoryService inventoryService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateInventoryAdjustmentCommandRequest, ApiResponse<CreateInventoryAdjustmentCommandResponse>>
    {
        public async Task<ApiResponse<CreateInventoryAdjustmentCommandResponse>> Handle(CreateInventoryAdjustmentCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<CreateInventoryAdjustmentCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            if (request.ArticleToken == Guid.Empty)
                return ApiResponse<CreateInventoryAdjustmentCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ArticleToken is required.", 400);

            if (request.DeltaQuantity == 0)
                return ApiResponse<CreateInventoryAdjustmentCommandResponse>.FailureResponse(ErrorCodes.InventoryInvalidAdjustment, "The adjustment quantity cannot be zero.", 400);

            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<CreateInventoryAdjustmentCommandResponse>.FailureResponse(ErrorCodes.InventoryInvalidAdjustment, "A reason is required for an inventory adjustment.", 400);

            var result = await inventoryService.CreateAdjustmentAsync(request.WarehouseToken, request.ArticleToken, request.DeltaQuantity, request.Reason, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateInventoryAdjustmentCommandResponse>.FailureResponse(ErrorCodes.InventoryWarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<CreateInventoryAdjustmentCommandResponse>.SuccessResponse(new CreateInventoryAdjustmentCommandResponse
            {
                StockLevel = mapper.Map<Responses.Common.StockLevel>(result)
            }, 201);
        }
    }
}
