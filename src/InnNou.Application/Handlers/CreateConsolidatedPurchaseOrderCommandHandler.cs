using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateConsolidatedPurchaseOrderCommandHandler(IConsolidatedPurchaseOrderService consolidatedPurchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateConsolidatedPurchaseOrderCommandRequest, ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>>
    {
        public async Task<ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>> Handle(CreateConsolidatedPurchaseOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.SupplierToken == Guid.Empty)
                return ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SupplierToken is required.", 400);

            if (request.DateTo < request.DateFrom)
                return ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.ConsolidatedPurchaseOrderInvalidDateRange, "DateTo must be on or after DateFrom.", 400);

            if (request.PurchaseOrderTokens is null || request.PurchaseOrderTokens.Count == 0)
                return ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.ConsolidatedPurchaseOrderEmpty, "At least one purchase order must be selected.", 400);

            var result = await consolidatedPurchaseOrderService.CreateAsync(
                request.SupplierToken, request.SuperAssociateOrganizationToken, request.Title, request.Notes,
                request.DateFrom, request.DateTo, request.PurchaseOrderTokens, context, cancellationToken);

            if (result is null)
                return ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>.FailureResponse(ErrorCodes.ConsolidatedPurchaseOrderNotFound, "Could not create the consolidation.", 404);

            return ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>.SuccessResponse(new CreateConsolidatedPurchaseOrderCommandResponse
            {
                ConsolidatedPurchaseOrder = mapper.Map<Responses.Common.ConsolidatedPurchaseOrder>(result)
            }, 201);
        }
    }
}
