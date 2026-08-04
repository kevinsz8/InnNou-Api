using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateParLevelCommandHandler(IParLevelService parLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateParLevelCommandRequest, ApiResponse<CreateParLevelCommandResponse>>
    {
        public async Task<ApiResponse<CreateParLevelCommandResponse>> Handle(CreateParLevelCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<CreateParLevelCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            if (request.ArticleToken == Guid.Empty)
                return ApiResponse<CreateParLevelCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ArticleToken is required.", 400);

            if (request.MinimumQuantity < 0)
                return ApiResponse<CreateParLevelCommandResponse>.FailureResponse(ErrorCodes.ParLevelInvalidQuantity, "Minimum quantity cannot be negative.", 400);

            if (request.ReorderQuantity <= 0)
                return ApiResponse<CreateParLevelCommandResponse>.FailureResponse(ErrorCodes.ParLevelInvalidQuantity, "Reorder quantity must be greater than zero.", 400);

            var result = await parLevelService.CreateBaseAsync(request.WarehouseToken, request.ArticleToken, request.MinimumQuantity, request.ReorderQuantity, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateParLevelCommandResponse>.FailureResponse(ErrorCodes.ParLevelWarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<CreateParLevelCommandResponse>.SuccessResponse(new CreateParLevelCommandResponse
            {
                ParLevel = mapper.Map<Responses.Common.ParLevel>(result)
            }, 201);
        }
    }
}
