using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteWarehouseCommandHandler(IWarehouseService warehouseService, IRequestContext context)
        : IRequestHandler<DeleteWarehouseCommandRequest, ApiResponse<DeleteWarehouseCommandResponse>>
    {
        public async Task<ApiResponse<DeleteWarehouseCommandResponse>> Handle(DeleteWarehouseCommandRequest request, CancellationToken cancellationToken)
        {
            var deleted = await warehouseService.DeleteAsync(request.WarehouseToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteWarehouseCommandResponse>.FailureResponse(ErrorCodes.WarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<DeleteWarehouseCommandResponse>.SuccessResponse(new DeleteWarehouseCommandResponse
            {
                WarehouseToken = request.WarehouseToken,
                Success = true
            });
        }
    }
}
