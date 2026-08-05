using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class RejectRequisitionCommandHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<RejectRequisitionCommandRequest, ApiResponse<RejectRequisitionCommandResponse>>
    {
        public async Task<ApiResponse<RejectRequisitionCommandResponse>> Handle(RejectRequisitionCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.RequisitionToken == Guid.Empty)
                return ApiResponse<RejectRequisitionCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "RequisitionToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<RejectRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionRejectReasonRequired, "A reason is required to reject a requisition.", 400);

            var result = await requisitionService.RejectAsync(request.RequisitionToken, request.Reason, context, cancellationToken);
            if (result is null)
                return ApiResponse<RejectRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionNotFound, "Requisition not found.", 404);

            return ApiResponse<RejectRequisitionCommandResponse>.SuccessResponse(new RejectRequisitionCommandResponse
            {
                Requisition = mapper.Map<Responses.Common.Requisition>(result)
            });
        }
    }
}
