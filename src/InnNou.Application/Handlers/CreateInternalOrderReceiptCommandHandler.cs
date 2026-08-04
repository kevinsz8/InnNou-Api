using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateInternalOrderReceiptCommandHandler(IInternalOrderService internalOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateInternalOrderReceiptCommandRequest, ApiResponse<CreateInternalOrderReceiptCommandResponse>>
    {
        public async Task<ApiResponse<CreateInternalOrderReceiptCommandResponse>> Handle(CreateInternalOrderReceiptCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.InternalOrderToken == Guid.Empty)
                return ApiResponse<CreateInternalOrderReceiptCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "InternalOrderToken is required.", 400);

            if (request.Lines is null || request.Lines.Count == 0)
                return ApiResponse<CreateInternalOrderReceiptCommandResponse>.FailureResponse(ErrorCodes.InternalOrderReceiptEmpty, "At least one line must be received.", 400);

            if (request.Lines.Any(l => l.QuantityAccepted < 0 || l.QuantityRejected < 0))
                return ApiResponse<CreateInternalOrderReceiptCommandResponse>.FailureResponse(ErrorCodes.InternalOrderReceiptLineEmpty, "Quantities cannot be negative.", 400);

            if (request.Lines.Any(l => l.QuantityAccepted + l.QuantityRejected <= 0))
                return ApiResponse<CreateInternalOrderReceiptCommandResponse>.FailureResponse(ErrorCodes.InternalOrderReceiptLineEmpty, "At least one quantity must be greater than zero for every line.", 400);

            if (request.Lines.Any(l => l.QuantityRejected > 0 && string.IsNullOrWhiteSpace(l.RejectionReason)))
                return ApiResponse<CreateInternalOrderReceiptCommandResponse>.FailureResponse(ErrorCodes.InternalOrderRejectionReasonRequired, "A rejection reason is required whenever a quantity is rejected.", 400);

            var lines = request.Lines.Select(l => new CreateInternalOrderReceiptLineInputDto
            {
                InternalOrderShipmentLineToken = l.InternalOrderShipmentLineToken,
                QuantityAccepted = l.QuantityAccepted,
                QuantityRejected = l.QuantityRejected,
                RejectionReason = l.RejectionReason,
                Notes = l.Notes
            }).ToList();

            var result = await internalOrderService.CreateReceiptAsync(request.InternalOrderToken, request.Notes, lines, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateInternalOrderReceiptCommandResponse>.FailureResponse(ErrorCodes.InternalOrderNotFound, "Internal order not found.", 404);

            return ApiResponse<CreateInternalOrderReceiptCommandResponse>.SuccessResponse(new CreateInternalOrderReceiptCommandResponse
            {
                InternalOrderReceipt = mapper.Map<Responses.Common.InternalOrderReceipt>(result)
            }, 201);
        }
    }
}
