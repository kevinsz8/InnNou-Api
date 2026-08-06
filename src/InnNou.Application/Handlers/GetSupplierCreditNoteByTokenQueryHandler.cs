using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetSupplierCreditNoteByTokenQueryHandler(ISupplierCreditNoteService supplierCreditNoteService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierCreditNoteByTokenQueryRequest, ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>> Handle(GetSupplierCreditNoteByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierCreditNoteService.GetByTokenAsync(request.SupplierCreditNoteToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>.FailureResponse(ErrorCodes.SupplierCreditNoteNotFound, "Supplier credit note not found.", 404);

            return ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>.SuccessResponse(
                new GetSupplierCreditNoteByTokenQueryResponse { SupplierCreditNote = mapper.Map<Responses.Common.SupplierCreditNote>(result) }, 200);
        }
    }

    // Returns success with a null SupplierCreditNote when the return has none yet — a legitimate
    // state (the SupplierReturn detail page uses this to decide between a "Ver Nota de Crédito"
    // link and a "Registrar Nota de Crédito" button), never a 404.
    public class GetSupplierCreditNoteBySupplierReturnTokenQueryHandler(ISupplierCreditNoteService supplierCreditNoteService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetSupplierCreditNoteBySupplierReturnTokenQueryRequest, ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>> Handle(GetSupplierCreditNoteBySupplierReturnTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await supplierCreditNoteService.GetBySupplierReturnTokenAsync(request.SupplierReturnToken, context, cancellationToken);
            return ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>.SuccessResponse(
                new GetSupplierCreditNoteByTokenQueryResponse { SupplierCreditNote = result is null ? null : mapper.Map<Responses.Common.SupplierCreditNote>(result) }, 200);
        }
    }
}
