using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class UpsertSupplierInvoiceMatchToleranceCommandHandler(ISupplierInvoiceService supplierInvoiceService, IMapper mapper, IRequestContext context)
        : IRequestHandler<UpsertSupplierInvoiceMatchToleranceCommandRequest, ApiResponse<UpsertSupplierInvoiceMatchToleranceCommandResponse>>
    {
        public async Task<ApiResponse<UpsertSupplierInvoiceMatchToleranceCommandResponse>> Handle(UpsertSupplierInvoiceMatchToleranceCommandRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierInvoiceService.UpsertToleranceAsync(request.OrganizationToken, request.TolerancePercent, request.ToleranceAmount, context, cancellationToken);
            if (result is null)
                return ApiResponse<UpsertSupplierInvoiceMatchToleranceCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceToleranceInvalid, "Tolerance could not be saved.", 500);

            var response = new UpsertSupplierInvoiceMatchToleranceCommandResponse { Tolerance = mapper.Map<Responses.Common.SupplierInvoiceMatchTolerance>(result) };
            return ApiResponse<UpsertSupplierInvoiceMatchToleranceCommandResponse>.SuccessResponse(response, 200);
        }
    }
}
