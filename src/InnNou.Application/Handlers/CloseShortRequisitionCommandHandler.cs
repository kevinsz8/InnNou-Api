using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CloseShortRequisitionCommandHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CloseShortRequisitionCommandRequest, ApiResponse<CloseShortRequisitionCommandResponse>>
    {
        public async Task<ApiResponse<CloseShortRequisitionCommandResponse>> Handle(CloseShortRequisitionCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.RequisitionToken == Guid.Empty)
                return ApiResponse<CloseShortRequisitionCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "RequisitionToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<CloseShortRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionCloseShortReasonRequired, "A reason is required to close a requisition as short.", 400);

            var result = await requisitionService.CloseShortAsync(request.RequisitionToken, request.Reason, context, cancellationToken);
            if (result is null)
                return ApiResponse<CloseShortRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionNotFound, "Requisition not found.", 404);

            return ApiResponse<CloseShortRequisitionCommandResponse>.SuccessResponse(new CloseShortRequisitionCommandResponse
            {
                Requisition = mapper.Map<Responses.Common.Requisition>(result)
            });
        }
    }
}
