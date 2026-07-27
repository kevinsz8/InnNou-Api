using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class SubmitInventoryPeriodCountCommandHandler(IInventoryPeriodService inventoryPeriodService, IMapper mapper, IRequestContext context)
        : IRequestHandler<SubmitInventoryPeriodCountCommandRequest, ApiResponse<SubmitInventoryPeriodCountCommandResponse>>
    {
        public async Task<ApiResponse<SubmitInventoryPeriodCountCommandResponse>> Handle(SubmitInventoryPeriodCountCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.InventoryPeriodToken == Guid.Empty)
                return ApiResponse<SubmitInventoryPeriodCountCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "InventoryPeriodToken is required.", 400);

            if (request.ArticleToken == Guid.Empty)
                return ApiResponse<SubmitInventoryPeriodCountCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ArticleToken is required.", 400);

            if (request.CountedQuantity < 0)
                return ApiResponse<SubmitInventoryPeriodCountCommandResponse>.FailureResponse(ErrorCodes.InventoryPeriodInvalidCount, "Counted quantity cannot be negative.", 400);

            var result = await inventoryPeriodService.SubmitCountAsync(request.InventoryPeriodToken, request.ArticleToken, request.CountedQuantity, context, cancellationToken);
            if (result is null)
                return ApiResponse<SubmitInventoryPeriodCountCommandResponse>.FailureResponse(ErrorCodes.InventoryPeriodNotFound, "Inventory period not found.", 404);

            return ApiResponse<SubmitInventoryPeriodCountCommandResponse>.SuccessResponse(new SubmitInventoryPeriodCountCommandResponse
            {
                InventoryPeriod = mapper.Map<Responses.Common.InventoryPeriod>(result)
            });
        }
    }
}
