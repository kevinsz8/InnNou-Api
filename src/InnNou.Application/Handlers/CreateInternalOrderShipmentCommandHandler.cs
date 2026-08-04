using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateInternalOrderShipmentCommandHandler(IInternalOrderService internalOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateInternalOrderShipmentCommandRequest, ApiResponse<CreateInternalOrderShipmentCommandResponse>>
    {
        public async Task<ApiResponse<CreateInternalOrderShipmentCommandResponse>> Handle(CreateInternalOrderShipmentCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.InternalOrderToken == Guid.Empty || request.SourceWarehouseToken == Guid.Empty)
                return ApiResponse<CreateInternalOrderShipmentCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "InternalOrderToken and SourceWarehouseToken are required.", 400);

            if (request.Lines is null || request.Lines.Count == 0)
                return ApiResponse<CreateInternalOrderShipmentCommandResponse>.FailureResponse(ErrorCodes.InternalOrderShipmentEmpty, "At least one line must be shipped.", 400);

            if (request.Lines.Any(l => l.QuantityShipped <= 0))
                return ApiResponse<CreateInternalOrderShipmentCommandResponse>.FailureResponse(ErrorCodes.InternalOrderInvalidQuantity, "Shipped quantity must be greater than zero.", 400);

            var lines = request.Lines.Select(l => new CreateInternalOrderShipmentLineInputDto
            {
                InternalOrderLineToken = l.InternalOrderLineToken,
                QuantityShipped = l.QuantityShipped,
                Notes = l.Notes
            }).ToList();

            var result = await internalOrderService.CreateShipmentAsync(request.InternalOrderToken, request.SourceWarehouseToken, request.Notes, lines, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateInternalOrderShipmentCommandResponse>.FailureResponse(ErrorCodes.InternalOrderNotFound, "Internal order not found.", 404);

            return ApiResponse<CreateInternalOrderShipmentCommandResponse>.SuccessResponse(new CreateInternalOrderShipmentCommandResponse
            {
                InternalOrderShipment = mapper.Map<Responses.Common.InternalOrderShipment>(result)
            }, 201);
        }
    }
}
