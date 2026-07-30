using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierInvoiceMatchToleranceQueryHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierInvoiceMatchToleranceQueryRequest, ApiResponse<GetSupplierInvoiceMatchToleranceQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierInvoiceMatchToleranceQueryResponse>> Handle(GetSupplierInvoiceMatchToleranceQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierInvoiceService.GetEffectiveToleranceAsync(request.OrganizationToken, context, cancellationToken);
            var response = new GetSupplierInvoiceMatchToleranceQueryResponse
            {
                Tolerance = result is null ? null : mapper.Map<Responses.Common.SupplierInvoiceMatchTolerance>(result)
            };
            return ApiResponse<GetSupplierInvoiceMatchToleranceQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
