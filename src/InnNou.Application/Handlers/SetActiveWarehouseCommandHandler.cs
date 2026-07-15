using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SetActiveWarehouseCommandHandler(IWarehouseService warehouseService, IRequestContext context)
        : IRequestHandler<SetActiveWarehouseCommandRequest, ApiResponse<SetActiveWarehouseCommandResponse>>
    {
        public async Task<ApiResponse<SetActiveWarehouseCommandResponse>> Handle(SetActiveWarehouseCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<SetActiveWarehouseCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            var result = await warehouseService.SetActiveAsync(request.WarehouseToken, request.IsActive, context, cancellationToken);
            if (result is null)
                return ApiResponse<SetActiveWarehouseCommandResponse>.FailureResponse(ErrorCodes.WarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<SetActiveWarehouseCommandResponse>.SuccessResponse(new SetActiveWarehouseCommandResponse
            {
                WarehouseToken = result.WarehouseToken,
                IsActive = result.IsActive
            });
        }
    }
}
