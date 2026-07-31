using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class UpsertSupplierInvoicePurchaseOrderPolicyCommandHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<UpsertSupplierInvoicePurchaseOrderPolicyCommandRequest, ApiResponse<UpsertSupplierInvoicePurchaseOrderPolicyCommandResponse>>
    {
        public async Task<ApiResponse<UpsertSupplierInvoicePurchaseOrderPolicyCommandResponse>> Handle(UpsertSupplierInvoicePurchaseOrderPolicyCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierInvoiceService.UpsertPurchaseOrderPolicyAsync(request.OrganizationToken, request.AllowMultiplePurchaseOrders, context, cancellationToken);
            if (result is null)
                return ApiResponse<UpsertSupplierInvoicePurchaseOrderPolicyCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoicePurchaseOrderPolicyInvalid, "Purchase order policy could not be saved.", 500);

            var response = new UpsertSupplierInvoicePurchaseOrderPolicyCommandResponse { Policy = mapper.Map<Responses.Common.SupplierInvoicePurchaseOrderPolicy>(result) };
            return ApiResponse<UpsertSupplierInvoicePurchaseOrderPolicyCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
