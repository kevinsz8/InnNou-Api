using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditWarehouseCommandHandler(IWarehouseService warehouseService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditWarehouseCommandRequest, ApiResponse<EditWarehouseCommandResponse>>
    {
        public async Task<ApiResponse<EditWarehouseCommandResponse>> Handle(EditWarehouseCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseToken == Guid.Empty)
                return ApiResponse<EditWarehouseCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<EditWarehouseCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Name is required.", 400);

            if (!string.IsNullOrWhiteSpace(request.PurposeCode) && !WarehousePurposeCodes.IsValid(request.PurposeCode))
                return ApiResponse<EditWarehouseCommandResponse>.FailureResponse(ErrorCodes.WarehouseInvalidPurpose, "PurposeCode is not one of the supported values.", 400);

            var dto = mapper.Map<WarehouseDto>(request);
            dto.PurposeCode = string.IsNullOrWhiteSpace(request.PurposeCode) ? WarehousePurposeCodes.General : request.PurposeCode.Trim().ToUpperInvariant();

            var result = await warehouseService.EditAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditWarehouseCommandResponse>.FailureResponse(ErrorCodes.WarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<EditWarehouseCommandResponse>.SuccessResponse(mapper.Map<EditWarehouseCommandResponse>(result));
        }
    }
}
