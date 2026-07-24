using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateGoodsReceiptCommandHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateGoodsReceiptCommandRequest, ApiResponse<CreateGoodsReceiptCommandResponse>>
    {
        public async Task<ApiResponse<CreateGoodsReceiptCommandResponse>> Handle(CreateGoodsReceiptCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.PurchaseOrderToken == Guid.Empty)
                return ApiResponse<CreateGoodsReceiptCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "PurchaseOrderToken is required.", 400);

            if (request.Lines is null || request.Lines.Count == 0)
                return ApiResponse<CreateGoodsReceiptCommandResponse>.FailureResponse(ErrorCodes.GoodsReceiptEmpty, "At least one line must be received.", 400);

            var lines = request.Lines.Select(l => new CreateGoodsReceiptLineInputDto
            {
                PurchaseOrderLineToken = l.PurchaseOrderLineToken,
                QuantityAccepted = l.QuantityAccepted,
                QuantityCourtesy = l.QuantityCourtesy,
                QuantityRejected = l.QuantityRejected,
                RejectionReason = l.RejectionReason,
                LotNumber = l.LotNumber,
                ExpirationDate = l.ExpirationDate,
                SerialNumber = l.SerialNumber,
                Notes = l.Notes
            }).ToList();

            var result = await purchaseOrderService.CreateGoodsReceiptAsync(request.PurchaseOrderToken, request.Notes, lines, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateGoodsReceiptCommandResponse>.FailureResponse(ErrorCodes.PurchaseOrderNotFound, "Purchase order not found.", 404);

            return ApiResponse<CreateGoodsReceiptCommandResponse>.SuccessResponse(new CreateGoodsReceiptCommandResponse
            {
                GoodsReceipt = mapper.Map<Responses.Common.GoodsReceipt>(result)
            }, 201);
        }
    }
}
