using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreatePurchaseOrderRectificationCommandHandler(IPurchaseOrderService purchaseOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreatePurchaseOrderRectificationCommandRequest, ApiResponse<CreatePurchaseOrderRectificationCommandResponse>>
    {
        public async Task<ApiResponse<CreatePurchaseOrderRectificationCommandResponse>> Handle(CreatePurchaseOrderRectificationCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.PurchaseOrderToken == Guid.Empty)
                return ApiResponse<CreatePurchaseOrderRectificationCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "PurchaseOrderToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<CreatePurchaseOrderRectificationCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Reason is required.", 400);

            if (!PurchaseOrderRectificationReasonCodes.TryFromCode(request.Reason, out _))
                return ApiResponse<CreatePurchaseOrderRectificationCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Invalid rectification reason.", 400);

            var requestedLines = request.Lines ?? [];
            var requestedNewLines = request.NewLines ?? [];

            if (requestedLines.Count == 0 && requestedNewLines.Count == 0)
                return ApiResponse<CreatePurchaseOrderRectificationCommandResponse>.FailureResponse(ErrorCodes.PurchaseOrderRectificationEmpty, "At least one line must be rectified or added.", 400);

            var lines = requestedLines.Select(l => new RectifyPurchaseOrderLineInputDto
            {
                PurchaseOrderLineToken = l.PurchaseOrderLineToken,
                Cancel = l.Cancel,
                NewQuantity = l.NewQuantity,
                NewUnitPrice = l.NewUnitPrice,
                NewCurrencyCode = l.NewCurrencyCode
            }).ToList();

            var newLines = requestedNewLines.Select(l => new RectifyPurchaseOrderNewLineInputDto
            {
                ArticleToken = l.ArticleToken,
                Quantity = l.Quantity,
                ManualUnitPrice = l.ManualUnitPrice,
                ManualCurrencyCode = l.ManualCurrencyCode
            }).ToList();

            var result = await purchaseOrderService.CreateRectificationAsync(request.PurchaseOrderToken, request.Reason, request.Notes, lines, newLines, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreatePurchaseOrderRectificationCommandResponse>.FailureResponse(ErrorCodes.PurchaseOrderNotFound, "Purchase order not found.", 404);

            return ApiResponse<CreatePurchaseOrderRectificationCommandResponse>.SuccessResponse(new CreatePurchaseOrderRectificationCommandResponse
            {
                PurchaseOrderRectification = mapper.Map<Responses.Common.PurchaseOrderRectification>(result)
            }, 201);
        }
    }
}
