using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierInvoiceByTokenQueryHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierInvoiceByTokenQueryRequest, ApiResponse<GetSupplierInvoiceByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierInvoiceByTokenQueryResponse>> Handle(GetSupplierInvoiceByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierInvoiceService.GetByTokenAsync(request.SupplierInvoiceToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetSupplierInvoiceByTokenQueryResponse>.FailureResponse(ErrorCodes.SupplierInvoiceNotFound, "Supplier invoice not found.", 404);

            var response = new GetSupplierInvoiceByTokenQueryResponse { SupplierInvoice = mapper.Map<Responses.Common.SupplierInvoice>(result) };
            return ApiResponse<GetSupplierInvoiceByTokenQueryResponse>.SuccessResponse(response, 200);
        }
    }
}
