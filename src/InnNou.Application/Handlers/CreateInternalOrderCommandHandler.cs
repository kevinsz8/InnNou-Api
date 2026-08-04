using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateInternalOrderCommandHandler(IInternalOrderService internalOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateInternalOrderCommandRequest, ApiResponse<CreateInternalOrderCommandResponse>>
    {
        public async Task<ApiResponse<CreateInternalOrderCommandResponse>> Handle(CreateInternalOrderCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.SourceOrganizationToken == Guid.Empty || request.DestinationWarehouseToken == Guid.Empty)
                return ApiResponse<CreateInternalOrderCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SourceOrganizationToken and DestinationWarehouseToken are required.", 400);

            if (request.Lines is null || request.Lines.Count == 0)
                return ApiResponse<CreateInternalOrderCommandResponse>.FailureResponse(ErrorCodes.InternalOrderEmpty, "At least one line must be requested.", 400);

            if (request.Lines.Any(l => l.Quantity <= 0))
                return ApiResponse<CreateInternalOrderCommandResponse>.FailureResponse(ErrorCodes.InternalOrderInvalidQuantity, "Requested quantity must be greater than zero.", 400);

            var lines = request.Lines.Select(l => new CreateInternalOrderLineInputDto
            {
                ArticleToken = l.ArticleToken,
                Quantity = l.Quantity,
                Notes = l.Notes
            }).ToList();

            var result = await internalOrderService.CreateAsync(request.SourceOrganizationToken, request.DestinationWarehouseToken, request.Notes, lines, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateInternalOrderCommandResponse>.FailureResponse(ErrorCodes.InternalOrderDestinationWarehouseNotFound, "Destination warehouse not found.", 404);

            return ApiResponse<CreateInternalOrderCommandResponse>.SuccessResponse(new CreateInternalOrderCommandResponse
            {
                InternalOrder = mapper.Map<Responses.Common.InternalOrder>(result)
            }, 201);
        }
    }
}
