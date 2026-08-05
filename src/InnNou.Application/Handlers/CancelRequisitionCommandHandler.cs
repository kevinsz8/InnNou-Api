using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CancelRequisitionCommandHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CancelRequisitionCommandRequest, ApiResponse<CancelRequisitionCommandResponse>>
    {
        public async Task<ApiResponse<CancelRequisitionCommandResponse>> Handle(CancelRequisitionCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.RequisitionToken == Guid.Empty)
                return ApiResponse<CancelRequisitionCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "RequisitionToken is required.", 400);

            var result = await requisitionService.CancelAsync(request.RequisitionToken, request.Reason, context, cancellationToken);
            if (result is null)
                return ApiResponse<CancelRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionNotFound, "Requisition not found.", 404);

            return ApiResponse<CancelRequisitionCommandResponse>.SuccessResponse(new CancelRequisitionCommandResponse
            {
                Requisition = mapper.Map<Responses.Common.Requisition>(result)
            });
        }
    }
}
