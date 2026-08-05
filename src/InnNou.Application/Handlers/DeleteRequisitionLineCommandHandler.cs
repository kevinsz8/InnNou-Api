using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DeleteRequisitionLineCommandHandler(IRequisitionService requisitionService, IRequestContext context)
        : IRequestHandler<DeleteRequisitionLineCommandRequest, ApiResponse<DeleteRequisitionLineCommandResponse>>
    {
        public async Task<ApiResponse<DeleteRequisitionLineCommandResponse>> Handle(DeleteRequisitionLineCommandRequest request, CancellationToken cancellationToken)
        {
            var deleted = await requisitionService.DeleteLineAsync(request.RequisitionLineToken, context, cancellationToken);
            if (!deleted)
                return ApiResponse<DeleteRequisitionLineCommandResponse>.FailureResponse(ErrorCodes.RequisitionLineNotFound, "Requisition line not found.", 404);

            return ApiResponse<DeleteRequisitionLineCommandResponse>.SuccessResponse(new DeleteRequisitionLineCommandResponse
            {
                RequisitionLineToken = request.RequisitionLineToken,
                Success = true
            });
        }
    }
}
