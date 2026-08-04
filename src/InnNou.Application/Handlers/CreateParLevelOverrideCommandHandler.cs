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

            if (request.MinimumQuantity < 0)
                return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.ParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);

            if (request.ReorderQuantity <= 0)
                return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.ParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

            ParLevelOverrideType type;
            try
            {
                type = ParLevelOverrideTypeCodes.FromCode(request.Type);
            }
            catch (ArgumentOutOfRangeException)
            {
                return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Type must be SEASONAL or EVENT.", 400);
            }

            if (type == ParLevelOverrideType.Seasonal)
            {
                if (request.StartMonth is null || request.StartDay is null || request.EndMonth is null || request.EndDay is null)
                    return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.ParLevelOverrideInvalidDateRange, "Start/end month and day are required for a seasonal override.", 400);

                if (!ParLevelDateValidation.IsValidMonthDay(request.StartMonth.Value, request.StartDay.Value) || !ParLevelDateValidation.IsValidMonthDay(request.EndMonth.Value, request.EndDay.Value))
                    return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.ParLevelOverrideInvalidDateRange, "Invalid start/end date — note Feb 29 is not supported as a seasonal boundary.", 400);
            }
            else
            {
                if (request.StartDate is null || request.EndDate is null)
                    return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.ParLevelOverrideInvalidDateRange, "Start/end date are required for an event override.", 400);

                if (request.StartDate.Value > request.EndDate.Value)
                    return ApiResponse<CreateParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.ParLevelOverrideInvalidDateRange, "The start date must not be after the end date.", 400);
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
