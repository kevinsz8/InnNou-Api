using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class CreateWarehouseCommandHandler(IWarehouseService warehouseService, IMapper mapper, IRequestContext context)
        : IRequestHandler<CreateWarehouseCommandRequest, ApiResponse<CreateWarehouseCommandResponse>>
    {
        public async Task<ApiResponse<CreateWarehouseCommandResponse>> Handle(CreateWarehouseCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.OrganizationToken == Guid.Empty)
                return ApiResponse<CreateWarehouseCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "OrganizationToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<CreateWarehouseCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Name is required.", 400);

            var dto = mapper.Map<WarehouseDto>(request);

            var result = await warehouseService.CreateAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<CreateWarehouseCommandResponse>.FailureResponse(ErrorCodes.WarehouseOrganizationNotFound, "Organization not found.", 404);

            return ApiResponse<CreateWarehouseCommandResponse>.SuccessResponse(mapper.Map<CreateWarehouseCommandResponse>(result), 201);
        }
    }
}
