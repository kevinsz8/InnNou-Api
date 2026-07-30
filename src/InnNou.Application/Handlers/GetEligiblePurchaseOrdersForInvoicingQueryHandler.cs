using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetEligiblePurchaseOrdersForInvoicingQueryHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetEligiblePurchaseOrdersForInvoicingQueryRequest, ApiResponse<GetEligiblePurchaseOrdersForInvoicingQueryResponse>>
    {
        public async Task<ApiResponse<GetEligiblePurchaseOrdersForInvoicingQueryResponse>> Handle(GetEligiblePurchaseOrdersForInvoicingQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierInvoiceService.GetEligiblePurchaseOrdersAsync(request.OrganizationToken, request.SupplierToken, context, cancellationToken);
            var response = new GetEligiblePurchaseOrdersForInvoicingQueryResponse { PurchaseOrders = mapper.MapList<Responses.Common.PurchaseOrder>(result) };
            return ApiResponse<GetEligiblePurchaseOrdersForInvoicingQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
