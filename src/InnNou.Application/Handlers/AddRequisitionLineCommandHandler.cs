using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class AddRequisitionLineCommandHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<AddRequisitionLineCommandRequest, ApiResponse<AddRequisitionLineCommandResponse>>
    {
        public async Task<ApiResponse<AddRequisitionLineCommandResponse>> Handle(AddRequisitionLineCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.RequisitionToken == Guid.Empty || request.ArticleToken == Guid.Empty)
                return ApiResponse<AddRequisitionLineCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "RequisitionToken and ArticleToken are required.", 400);

            if (request.QuantityRequested <= 0)
                return ApiResponse<AddRequisitionLineCommandResponse>.FailureResponse(ErrorCodes.RequisitionInvalidQuantity, "Requested quantity must be greater than zero.", 400);

            var result = await requisitionService.AddLineAsync(request.RequisitionToken, request.ArticleToken, request.QuantityRequested, request.UnitToken, request.Notes, context, cancellationToken);
            if (result is null)
                return ApiResponse<AddRequisitionLineCommandResponse>.FailureResponse(ErrorCodes.RequisitionNotFound, "Requisition not found.", 404);

            return ApiResponse<AddRequisitionLineCommandResponse>.SuccessResponse(new AddRequisitionLineCommandResponse
            {
                RequisitionLine = mapper.Map<Responses.Common.RequisitionLine>(result)
            }, 201);
        }
    }
}
