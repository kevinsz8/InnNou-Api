using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class OpenInventoryPeriodCommandHandler(IInventoryPeriodService inventoryPeriodService, IMapper mapper, IRequestContext context)
        : IRequestHandler<OpenInventoryPeriodCommandRequest, ApiResponse<OpenInventoryPeriodCommandResponse>>
    {
        public async Task<ApiResponse<OpenInventoryPeriodCommandResponse>> Handle(OpenInventoryPeriodCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<OpenInventoryPeriodCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            var result = await inventoryPeriodService.OpenAsync(request.WarehouseToken, request.Notes, context, cancellationToken);
            if (result is null)
                return ApiResponse<OpenInventoryPeriodCommandResponse>.FailureResponse(ErrorCodes.InventoryWarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<OpenInventoryPeriodCommandResponse>.SuccessResponse(new OpenInventoryPeriodCommandResponse
            {
                InventoryPeriod = mapper.Map<Responses.Common.InventoryPeriod>(result)
            }, 201);
        }
    }
}
