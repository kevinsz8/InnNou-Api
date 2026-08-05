using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class ApproveRequisitionCommandHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<ApproveRequisitionCommandRequest, ApiResponse<ApproveRequisitionCommandResponse>>
    {
        public async Task<ApiResponse<ApproveRequisitionCommandResponse>> Handle(ApproveRequisitionCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.RequisitionToken == Guid.Empty)
                return ApiResponse<ApproveRequisitionCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "RequisitionToken is required.", 400);

            var result = await requisitionService.ApproveAsync(request.RequisitionToken, context, cancellationToken);
            if (result is null)
                return ApiResponse<ApproveRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionNotFound, "Requisition not found.", 404);

            return ApiResponse<ApproveRequisitionCommandResponse>.SuccessResponse(new ApproveRequisitionCommandResponse
            {
                Requisition = mapper.Map<Responses.Common.Requisition>(result)
            });
        }
    }
}
