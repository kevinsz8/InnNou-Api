using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditRequisitionLineCommandHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditRequisitionLineCommandRequest, ApiResponse<EditRequisitionLineCommandResponse>>
    {
        public async Task<ApiResponse<EditRequisitionLineCommandResponse>> Handle(EditRequisitionLineCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.RequisitionLineToken == Guid.Empty)
                return ApiResponse<EditRequisitionLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "RequisitionLineToken is required.", 400);

            if (request.QuantityRequested <= 0)
                return ApiResponse<EditRequisitionLineCommandResponse>.FailureResponse(ErrorCodes.RequisitionInvalidQuantity, "Requested quantity must be greater than zero.", 400);

            var result = await requisitionService.EditLineAsync(request.RequisitionLineToken, request.QuantityRequested, request.UnitToken, request.Notes, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditRequisitionLineCommandResponse>.FailureResponse(ErrorCodes.RequisitionLineNotFound, "Requisition line not found.", 404);

            return ApiResponse<EditRequisitionLineCommandResponse>.SuccessResponse(new EditRequisitionLineCommandResponse
            {
                RequisitionLine = mapper.Map<Responses.Common.RequisitionLine>(result)
            });
        }
    }
}
