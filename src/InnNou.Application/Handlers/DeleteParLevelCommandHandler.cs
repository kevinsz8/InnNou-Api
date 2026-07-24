using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteParLevelCommandHandler(IParLevelService parLevelService, IRequestContext context)
        : IRequestHandler<DeleteParLevelCommandRequest, ApiResponse<DeleteParLevelCommandResponse>>
    {
        public async Task<ApiResponse<DeleteParLevelCommandResponse>> Handle(DeleteParLevelCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.ParLevelToken == Guid.Empty)
                return ApiResponse<DeleteParLevelCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ParLevelToken is required.", 400);

            var deleted = await parLevelService.DeleteBaseAsync(request.ParLevelToken, context, cancellationToken);
            return ApiResponse<DeleteParLevelCommandResponse>.SuccessResponse(new DeleteParLevelCommandResponse { Deleted = deleted }, 200);
        }
    }
}
