using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteOrderTemplateCommandHandler(IOrderTemplateService orderTemplateService, IRequestContext context)
        : IRequestHandler<DeleteOrderTemplateCommandRequest, ApiResponse<DeleteOrderTemplateCommandResponse>>
    {
        public async Task<ApiResponse<DeleteOrderTemplateCommandResponse>> Handle(DeleteOrderTemplateCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderTemplateToken == Guid.Empty)
                return ApiResponse<DeleteOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderTemplateToken is required.", 400);

            var deleted = await orderTemplateService.DeleteAsync(request.OrderTemplateToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.OrderTemplateNotFound, "Order template not found.", 404);

            return ApiResponse<DeleteOrderTemplateCommandResponse>.SuccessResponse(new DeleteOrderTemplateCommandResponse
            {
                OrderTemplateToken = request.OrderTemplateToken,
                Success = true
            });
        }
    }
}
