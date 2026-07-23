using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetConsolidatedPurchaseOrderByTokenQueryHandler(IConsolidatedPurchaseOrderService consolidatedPurchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetConsolidatedPurchaseOrderByTokenQueryRequest, ApiResponse<GetConsolidatedPurchaseOrderByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetConsolidatedPurchaseOrderByTokenQueryResponse>> Handle(GetConsolidatedPurchaseOrderByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await consolidatedPurchaseOrderService.GetByTokenAsync(request.ConsolidatedPurchaseOrderToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetConsolidatedPurchaseOrderByTokenQueryResponse>.FailureResponse(ErrorCodes.ConsolidatedPurchaseOrderNotFound, "Consolidated purchase order not found.", 404);

            return ApiResponse<GetConsolidatedPurchaseOrderByTokenQueryResponse>.SuccessResponse(new GetConsolidatedPurchaseOrderByTokenQueryResponse
            {
                ConsolidatedPurchaseOrder = mapper.Map<Responses.Common.ConsolidatedPurchaseOrder>(result)
            });
        }
    }
}
