using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditParLevelCommandHandler(IParLevelService parLevelService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditParLevelCommandRequest, ApiResponse<EditParLevelCommandResponse>>
    {
        public async Task<ApiResponse<EditParLevelCommandResponse>> Handle(EditParLevelCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.ParLevelToken == Guid.Empty)
                return ApiResponse<EditParLevelCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ParLevelToken is required.", 400);

            var result = await parLevelService.EditBaseAsync(request.ParLevelToken, request.MinimumQuantity, request.ReorderQuantity, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditParLevelCommandResponse>.FailureResponse(ErrorCodes.ParLevelNotFound, "Par level not found.", 404);

            return ApiResponse<EditParLevelCommandResponse>.SuccessResponse(new EditParLevelCommandResponse
            {
                ParLevel = mapper.Map<Responses.Common.ParLevel>(result)
            }, 200);
        }
    }
}
