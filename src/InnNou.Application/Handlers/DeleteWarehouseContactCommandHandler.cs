using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteWarehouseContactCommandHandler(IWarehouseContactService warehouseContactService, IRequestContext context)
        : IRequestHandler<DeleteWarehouseContactCommandRequest, ApiResponse<DeleteWarehouseContactCommandResponse>>
    {
        public async Task<ApiResponse<DeleteWarehouseContactCommandResponse>> Handle(DeleteWarehouseContactCommandRequest request, CancellationToken cancellationToken)
        {
            var deleted = await warehouseContactService.DeleteAsync(request.WarehouseContactToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteWarehouseContactCommandResponse>.FailureResponse(ErrorCodes.WarehouseContactNotFound, "Warehouse contact not found.", 404);

            return ApiResponse<DeleteWarehouseContactCommandResponse>.SuccessResponse(new DeleteWarehouseContactCommandResponse
            {
                WarehouseContactToken = request.WarehouseContactToken,
                Success = true
            });
        }
    }
}
