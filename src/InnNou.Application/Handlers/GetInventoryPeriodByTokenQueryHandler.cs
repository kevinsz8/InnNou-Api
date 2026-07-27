using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInventoryPeriodByTokenQueryHandler(IInventoryPeriodService inventoryPeriodService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetInventoryPeriodByTokenQueryRequest, ApiResponse<GetInventoryPeriodByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetInventoryPeriodByTokenQueryResponse>> Handle(GetInventoryPeriodByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.InventoryPeriodToken == Guid.Empty)
                return ApiResponse<GetInventoryPeriodByTokenQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "InventoryPeriodToken is required.", 400);

            var result = await inventoryPeriodService.GetByTokenAsync(request.InventoryPeriodToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetInventoryPeriodByTokenQueryResponse>.FailureResponse(ErrorCodes.InventoryPeriodNotFound, "Inventory period not found.", 404);

            return ApiResponse<GetInventoryPeriodByTokenQueryResponse>.SuccessResponse(new GetInventoryPeriodByTokenQueryResponse
            {
                InventoryPeriod = mapper.Map<Responses.Common.InventoryPeriod>(result)
            });
        }
    }
}
