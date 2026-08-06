using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateSupplierCreditNoteCommandHandler(ISupplierCreditNoteService supplierCreditNoteService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateSupplierCreditNoteCommandRequest, ApiResponse<CreateSupplierCreditNoteCommandResponse>>
    {
        public async Task<ApiResponse<CreateSupplierCreditNoteCommandResponse>> Handle(CreateSupplierCreditNoteCommandRequest request, CancellationToken cancellationToken)
        {
            var lines = request.Lines.Select(l => new CreateSupplierCreditNoteLineInputDto
            {
                SupplierReturnLineToken = l.SupplierReturnLineToken,
                UnitPrice = l.UnitPrice
            }).ToList();

            var result = await supplierCreditNoteService.CreateAsync(
                request.SupplierReturnToken, request.CreditNoteNumber, request.CreditNoteDate, request.Reason, request.Notes, lines, context, cancellationToken);

            if (result is null)
                return ApiResponse<CreateSupplierCreditNoteCommandResponse>.FailureResponse(ErrorCodes.SupplierCreditNoteReturnNotFound, "Supplier return not found.", 404);

            return ApiResponse<CreateSupplierCreditNoteCommandResponse>.SuccessResponse(
                new CreateSupplierCreditNoteCommandResponse { SupplierCreditNote = mapper.Map<Responses.Common.SupplierCreditNote>(result) }, 201);
        }
    }
}
