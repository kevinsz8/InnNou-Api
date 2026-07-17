using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class RenameOrderTemplateCommandHandler(IOrderTemplateService orderTemplateService, IMapper mapper, IRequestContext context)
        : IRequestHandler<RenameOrderTemplateCommandRequest, ApiResponse<RenameOrderTemplateCommandResponse>>
    {
        public async Task<ApiResponse<RenameOrderTemplateCommandResponse>> Handle(RenameOrderTemplateCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrderTemplateToken == Guid.Empty)
                return ApiResponse<RenameOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrderTemplateToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<RenameOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Name is required.", 400);

            var template = await orderTemplateService.RenameAsync(request.OrderTemplateToken, request.Name.Trim(), context, cancellationToken);
            if (template is null)
                return ApiResponse<RenameOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.OrderTemplateNotFound, "Order template not found.", 404);

            return ApiResponse<RenameOrderTemplateCommandResponse>.SuccessResponse(new RenameOrderTemplateCommandResponse
            {
                OrderTemplate = mapper.Map<Responses.Common.OrderTemplate>(template)
            });
        }
    }
}
