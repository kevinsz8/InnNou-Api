using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ImportOrderLinesCommandHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<ImportOrderLinesCommandRequest, ApiResponse<ImportOrderLinesCommandResponse>>
    {
        public async Task<ApiResponse<ImportOrderLinesCommandResponse>> Handle(ImportOrderLinesCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderToken == Guid.Empty)
                return ApiResponse<ImportOrderLinesCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderToken is required.", 400);

            var result = await orderService.ImportLinesAsync(request.OrderToken, request.FileBytes, context, cancellationToken);
            var response = mapper.Map<ImportOrderLinesCommandResponse>(result);
            return ApiResponse<ImportOrderLinesCommandResponse>.SuccessResponse(response);
        }
    }
}
