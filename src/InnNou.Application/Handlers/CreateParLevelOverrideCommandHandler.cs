using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateParLevelOverrideCommandHandler(IParLevelService parLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateParLevelOverrideCommandRequest, ApiResponse<CreateParLevelOverrideCommandResponse>>
    {
        public async Task<ApiResponse<CreateParLevelOverrideCommandResponse>> Handle(CreateParLevelOverrideCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            if (request.ArticleToken == Guid.Empty)
                return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ArticleToken is required.", 400);

            ParLevelOverrideType type;
            try
            {
                type = ParLevelOverrideTypeCodes.FromCode(request.Type);
            }
            catch (ArgumentOutOfRangeException)
            {
                return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Type must be SEASONAL or EVENT.", 400);
            }

            var result = await parLevelService.CreateOverrideAsync(
                request.WarehouseToken, request.ArticleToken, type, request.Label,
                request.MinimumQuantity, request.ReorderQuantity,
                request.StartMonth, request.StartDay, request.EndMonth, request.EndDay,
                request.StartDate, request.EndDate,
                context, cancellationToken);

            if (result is null)
                return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.ParLevelWarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<CreateParLevelOverrideCommandResponse>.SuccessResponse(new CreateParLevelOverrideCommandResponse
            {
                Override = mapper.Map<Responses.Common.ParLevelOverride>(result)
            }, 201);
        }
    }
}
