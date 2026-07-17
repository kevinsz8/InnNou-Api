using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateOrderTemplateCommandHandler(IOrderTemplateService orderTemplateService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateOrderTemplateCommandRequest, ApiResponse<CreateOrderTemplateCommandResponse>>
    {
        public async Task<ApiResponse<CreateOrderTemplateCommandResponse>> Handle(CreateOrderTemplateCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<CreateOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<CreateOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Name is required.", 400);

            var template = await orderTemplateService.CreateAsync(request.WarehouseToken, request.Name.Trim(), context, cancellationToken);
            if (template is null)
                return ApiResponse<CreateOrderTemplateCommandResponse>.FailureResponse(ErrorCodes.OrderTemplateWarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<CreateOrderTemplateCommandResponse>.SuccessResponse(new CreateOrderTemplateCommandResponse
            {
                OrderTemplate = mapper.Map<Responses.Common.OrderTemplate>(template)
            }, 201);
        }
    }
}
