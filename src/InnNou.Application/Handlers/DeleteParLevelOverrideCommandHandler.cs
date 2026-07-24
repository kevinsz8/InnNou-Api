using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteParLevelOverrideCommandHandler(IParLevelService parLevelService, IRequestContext context)
        : IRequestHandler<DeleteParLevelOverrideCommandRequest, ApiResponse<DeleteParLevelOverrideCommandResponse>>
    {
        public async Task<ApiResponse<DeleteParLevelOverrideCommandResponse>> Handle(DeleteParLevelOverrideCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.ParLevelOverrideToken == Guid.Empty)
                return ApiResponse<DeleteParLevelOverrideCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ParLevelOverrideToken is required.", 400);

            var deleted = await parLevelService.DeleteOverrideAsync(request.ParLevelOverrideToken, context, cancellationToken);
            return ApiResponse<DeleteParLevelOverrideCommandResponse>.SuccessResponse(new DeleteParLevelOverrideCommandResponse { Deleted = deleted }, 200);
        }
    }
}
