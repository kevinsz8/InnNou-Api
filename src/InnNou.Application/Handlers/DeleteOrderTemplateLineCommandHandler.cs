using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteOrderTemplateLineCommandHandler(IOrderTemplateService orderTemplateService, IRequestContext context)
        : IRequestHandler<DeleteOrderTemplateLineCommandRequest, ApiResponse<DeleteOrderTemplateLineCommandResponse>>
    {
        public async Task<ApiResponse<DeleteOrderTemplateLineCommandResponse>> Handle(DeleteOrderTemplateLineCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderTemplateLineToken == Guid.Empty)
                return ApiResponse<DeleteOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderTemplateLineToken is required.", 400);

            var deleted = await orderTemplateService.DeleteLineAsync(request.OrderTemplateLineToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteOrderTemplateLineCommandResponse>.FailureResponse(ErrorCodes.OrderTemplateLineNotFound, "Order template line not found.", 404);

            return ApiResponse<DeleteOrderTemplateLineCommandResponse>.SuccessResponse(new DeleteOrderTemplateLineCommandResponse
            {
                OrderTemplateLineToken = request.OrderTemplateLineToken,
                Success = true
            });
        }
    }
}
