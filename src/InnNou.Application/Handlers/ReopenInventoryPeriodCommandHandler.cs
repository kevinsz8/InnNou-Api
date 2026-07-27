using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ReopenInventoryPeriodCommandHandler(IInventoryPeriodService inventoryPeriodService, IMapper mapper, IRequestContext context)
        : IRequestHandler<ReopenInventoryPeriodCommandRequest, ApiResponse<ReopenInventoryPeriodCommandResponse>>
    {
        public async Task<ApiResponse<ReopenInventoryPeriodCommandResponse>> Handle(ReopenInventoryPeriodCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.InventoryPeriodToken == Guid.Empty)
                return ApiResponse<ReopenInventoryPeriodCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "InventoryPeriodToken is required.", 400);

            var result = await inventoryPeriodService.ReopenAsync(request.InventoryPeriodToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<ReopenInventoryPeriodCommandResponse>.FailureResponse(ErrorCodes.InventoryPeriodNotFound, "Inventory period not found.", 404);

            return ApiResponse<ReopenInventoryPeriodCommandResponse>.SuccessResponse(new ReopenInventoryPeriodCommandResponse
            {
                InventoryPeriod = mapper.Map<Responses.Common.InventoryPeriod>(result)
            });
        }
    }
}
