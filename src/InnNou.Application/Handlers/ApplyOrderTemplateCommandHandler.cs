using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ApplyOrderTemplateCommandHandler(IOrderTemplateService orderTemplateService, IMapper mapper, IRequestContext context)
        : IRequestHandler<ApplyOrderTemplateCommandRequest, ApiResponse<ApplyOrderTemplateCommandResponse>>
    {
        public async Task<ApiResponse<ApplyOrderTemplateCommandResponse>> Handle(ApplyOrderTemplateCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderTemplateToken == Guid.Empty)
                return ApiResponse<ApplyOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderTemplateToken is required.", 400);

            if (request.OrderToken == Guid.Empty)
                return ApiResponse<ApplyOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderToken is required.", 400);

            var result = await orderTemplateService.ApplyToOrderAsync(request.OrderTemplateToken, request.OrderToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<ApplyOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.OrderTemplateNotFound, "Order template not found.", 404);

            var response = mapper.Map<ApplyOrderTemplateCommandResponse>(result);
            return ApiResponse<ApplyOrderTemplateCommandResponse>.SuccessResponse(response);
        }
    }
}
