using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetParLevelConfigurationQueryHandler(IParLevelService parLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetParLevelConfigurationQueryRequest, ApiResponse<GetParLevelConfigurationQueryResponse>>
    {
        public async Task<ApiResponse<GetParLevelConfigurationQueryResponse>> Handle(GetParLevelConfigurationQueryRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<GetParLevelConfigurationQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            if (request.ArticleToken == Guid.Empty)
                return ApiResponse<GetParLevelConfigurationQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ArticleToken is required.", 400);

            var result = await parLevelService.GetConfigurationAsync(request.WarehouseToken, request.ArticleToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<GetParLevelConfigurationQueryResponse>.FailureResponse(ErrorCodes.ParLevelWarehouseNotFound, "Warehouse or article not found, or outside your scope.", 404);

            var configuration = new Responses.Common.ParLevelConfiguration
            {
                Base = result.Base is null ? null : mapper.Map<Responses.Common.ParLevel>(result.Base),
                Overrides = mapper.MapList<Responses.Common.ParLevelOverride>(result.Overrides),
                EffectiveToday = result.EffectiveToday is null ? null : mapper.Map<Responses.Common.ParLevelEffective>(result.EffectiveToday)
            };

            return ApiResponse<GetParLevelConfigurationQueryResponse>.SuccessResponse(new GetParLevelConfigurationQueryResponse
            {
                Configuration = configuration
            });
        }
    }
}
