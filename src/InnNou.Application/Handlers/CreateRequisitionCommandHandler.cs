using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateRequisitionCommandHandler(IRequisitionService requisitionService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateRequisitionCommandRequest, ApiResponse<CreateRequisitionCommandResponse>>
    {
        public async Task<ApiResponse<CreateRequisitionCommandResponse>> Handle(CreateRequisitionCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty || request.DepartmentToken == Guid.Empty)
                return ApiResponse<CreateRequisitionCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken and DepartmentToken are required.", 400);

            if (request.Lines is null || request.Lines.Count == 0)
                return ApiResponse<CreateRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionEmpty, "At least one line must be requested.", 400);

            if (request.Lines.Any(l => l.QuantityRequested <= 0))
                return ApiResponse<CreateRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionInvalidQuantity, "Requested quantity must be greater than zero.", 400);

            var lines = request.Lines.Select(l => new CreateRequisitionLineInputDto
            {
                ArticleToken = l.ArticleToken,
                QuantityRequested = l.QuantityRequested,
                Notes = l.Notes
            }).ToList();

            var result = await requisitionService.CreateAsync(request.WarehouseToken, request.DepartmentToken, request.Notes, lines, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateRequisitionCommandResponse>.FailureResponse(ErrorCodes.RequisitionWarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<CreateRequisitionCommandResponse>.SuccessResponse(new CreateRequisitionCommandResponse
            {
                Requisition = mapper.Map<Responses.Common.Requisition>(result)
            }, 201);
        }
    }
}
