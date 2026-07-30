using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateSupplierReturnCommandHandler(ISupplierReturnService supplierReturnService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateSupplierReturnCommandRequest, ApiResponse<CreateSupplierReturnCommandResponse>>
    {
        public async Task<ApiResponse<CreateSupplierReturnCommandResponse>> Handle(CreateSupplierReturnCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.PurchaseOrderToken == Guid.Empty)
                return ApiResponse<CreateSupplierReturnCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "PurchaseOrderToken is required.", 400);

            if (request.GoodsReceiptLineTokens is null || request.GoodsReceiptLineTokens.Count == 0)
                return ApiResponse<CreateSupplierReturnCommandResponse>.FailureResponse(ErrorCodes.SupplierReturnEmpty, "At least one rejected line must be included.", 400);

            var result = await supplierReturnService.CreateAsync(request.PurchaseOrderToken, request.Notes, request.GoodsReceiptLineTokens, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateSupplierReturnCommandResponse>.FailureResponse(ErrorCodes.PurchaseOrderNotFound, "Purchase order not found.", 404);

            return ApiResponse<CreateSupplierReturnCommandResponse>.SuccessResponse(new CreateSupplierReturnCommandResponse
            {
                SupplierReturn = mapper.Map<Responses.Common.SupplierReturn>(result)
            }, 201);
        }
    }
}
