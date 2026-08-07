using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class AddOrderLinesCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<AddOrderLinesCommandRequest, ApiResponse<AddOrderLinesCommandResponse>>
    {
        // Deliberately smaller than MaxBulkImportRows (500, OrderService's Excel-import cap) —
        // this is an interactive UI batch (a buyer selecting several catalog items at once), not
        // a spreadsheet upload.
        private const int MaxLines = 100;

        public async Task<ApiResponse<AddOrderLinesCommandResponse>> Handle(AddOrderLinesCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderToken == Guid.Empty)
                return ApiResponse<AddOrderLinesCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderToken is required.", 400);

            if (request.Lines.Count == 0)
                return ApiResponse<AddOrderLinesCommandResponse>.FailureResponse(ErrorCodes.OrderAddLinesEmpty, "At least one line is required.", 400);

            if (request.Lines.Count > MaxLines)
                return ApiResponse<AddOrderLinesCommandResponse>.FailureResponse(ErrorCodes.OrderAddLinesTooMany, $"A single request cannot add more than {MaxLines} lines.", 400);

            var result = await orderService.AddLinesAsync(request.OrderToken, request.Lines, context, cancellationToken);
            var response = mapper.Map<AddOrderLinesCommandResponse>(result);
            return ApiResponse<AddOrderLinesCommandResponse>.SuccessResponse(response);
        }
    }
}
