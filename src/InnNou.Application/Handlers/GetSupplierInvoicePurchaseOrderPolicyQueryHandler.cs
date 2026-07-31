using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierInvoicePurchaseOrderPolicyQueryHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierInvoicePurchaseOrderPolicyQueryRequest, ApiResponse<GetSupplierInvoicePurchaseOrderPolicyQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierInvoicePurchaseOrderPolicyQueryResponse>> Handle(GetSupplierInvoicePurchaseOrderPolicyQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierInvoiceService.GetEffectivePurchaseOrderPolicyAsync(request.OrganizationToken, context, cancellationToken);
            var response = new GetSupplierInvoicePurchaseOrderPolicyQueryResponse
            {
                Policy = result is null ? null : mapper.Map<Responses.Common.SupplierInvoicePurchaseOrderPolicy>(result)
            };
            return ApiResponse<GetSupplierInvoicePurchaseOrderPolicyQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
