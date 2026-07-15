using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateWarehouseContactCommandHandler(IWarehouseContactService warehouseContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateWarehouseContactCommandRequest, ApiResponse<CreateWarehouseContactCommandResponse>>
    {
        public async Task<ApiResponse<CreateWarehouseContactCommandResponse>> Handle(CreateWarehouseContactCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<CreateWarehouseContactCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.ContactName))
                return ApiResponse<CreateWarehouseContactCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ContactName is required.", 400);

            var dto = mapper.Map<WarehouseContactDto>(request);
            var result = await warehouseContactService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateWarehouseContactCommandResponse>.FailureResponse(ErrorCodes.WarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<CreateWarehouseContactCommandResponse>.SuccessResponse(mapper.Map<CreateWarehouseContactCommandResponse>(result), 201);
        }
    }
}
