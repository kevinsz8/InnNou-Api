using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetConsolidatedPurchaseOrderCandidatesQueryHandler(IConsolidatedPurchaseOrderService consolidatedPurchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetConsolidatedPurchaseOrderCandidatesQueryRequest, ApiResponse<GetConsolidatedPurchaseOrderCandidatesQueryResponse>>
    {
        public async Task<ApiResponse<GetConsolidatedPurchaseOrderCandidatesQueryResponse>> Handle(GetConsolidatedPurchaseOrderCandidatesQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.SupplierToken == Guid.Empty)
                return ApiResponse<GetConsolidatedPurchaseOrderCandidatesQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SupplierToken is required.", 400);

            if (request.DateTo < request.DateFrom)
                return ApiResponse<GetConsolidatedPurchaseOrderCandidatesQueryResponse>.FailureResponse(ErrorCodes.ConsolidatedPurchaseOrderInvalidDateRange, "DateTo must be on or after DateFrom.", 400);

            var result = await consolidatedPurchaseOrderService.GetCandidatesAsync(
                request.SupplierToken, request.SuperAssociateOrganizationToken, request.DateFrom, request.DateTo, context, cancellationToken);

            return ApiResponse<GetConsolidatedPurchaseOrderCandidatesQueryResponse>.SuccessResponse(new GetConsolidatedPurchaseOrderCandidatesQueryResponse
            {
                PurchaseOrders = mapper.MapList<Responses.Common.PurchaseOrder>(result)
            });
        }
    }
}
