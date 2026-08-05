using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateRequisitionIssueCommandHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateRequisitionIssueCommandRequest, ApiResponse<CreateRequisitionIssueCommandResponse>>
    {
        public async Task<ApiResponse<CreateRequisitionIssueCommandResponse>> Handle(CreateRequisitionIssueCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.RequisitionToken == Guid.Empty)
                return ApiResponse<CreateRequisitionIssueCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "RequisitionToken is required.", 400);

            if (request.Lines is null || request.Lines.Count == 0)
                return ApiResponse<CreateRequisitionIssueCommandResponse>.FailureResponse(ErrorCodes.RequisitionIssueEmpty, "At least one line must be issued.", 400);

            if (request.Lines.Any(l => l.QuantityIssued <= 0))
                return ApiResponse<CreateRequisitionIssueCommandResponse>.FailureResponse(ErrorCodes.RequisitionInvalidQuantity, "Issued quantity must be greater than zero.", 400);

            var lines = request.Lines.Select(l => new CreateRequisitionIssueLineInputDto
            {
                RequisitionLineToken = l.RequisitionLineToken,
                QuantityIssued = l.QuantityIssued,
                Notes = l.Notes
            }).ToList();

            var result = await requisitionService.CreateIssueAsync(request.RequisitionToken, request.Notes, lines, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateRequisitionIssueCommandResponse>.FailureResponse(ErrorCodes.RequisitionNotFound, "Requisition not found.", 404);

            return ApiResponse<CreateRequisitionIssueCommandResponse>.SuccessResponse(new CreateRequisitionIssueCommandResponse
            {
                RequisitionIssue = mapper.Map<Responses.Common.RequisitionIssue>(result)
            }, 201);
        }
    }
}
