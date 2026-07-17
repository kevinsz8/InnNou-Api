using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrderTemplateByTokenQueryHandler(IOrderTemplateService orderTemplateService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetOrderTemplateByTokenQueryRequest, ApiResponse<GetOrderTemplateByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetOrderTemplateByTokenQueryResponse>> Handle(GetOrderTemplateByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var template = await orderTemplateService.GetByTokenAsync(request.OrderTemplateToken, context, cancellationToken);
            if (template is null)
                return ApiResponse<GetOrderTemplateByTokenQueryResponse>.FailureResponse(ErrorCodes.OrderTemplateNotFound, "Order template not found.", 404);

            return ApiResponse<GetOrderTemplateByTokenQueryResponse>.SuccessResponse(new GetOrderTemplateByTokenQueryResponse
            {
                OrderTemplate = mapper.Map<Responses.Common.OrderTemplate>(template)
            });
        }
    }
}
