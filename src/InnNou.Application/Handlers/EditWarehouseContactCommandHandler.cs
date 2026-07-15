using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class EditWarehouseContactCommandHandler(IWarehouseContactService warehouseContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<EditWarehouseContactCommandRequest, ApiResponse<EditWarehouseContactCommandResponse>>
    {
        public async Task<ApiResponse<EditWarehouseContactCommandResponse>> Handle(EditWarehouseContactCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.WarehouseContactToken == Guid.Empty)
                return ApiResponse<EditWarehouseContactCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "WarehouseContactToken is required.", 400);

            if (string.IsNullOrWhiteSpace(request.ContactName))
                return ApiResponse<EditWarehouseContactCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "ContactName is required.", 400);

            var dto = mapper.Map<WarehouseContactDto>(request);
            var result = await warehouseContactService.EditAsync(dto, context, cancellationToken);
            if (result is null)
                return ApiResponse<EditWarehouseContactCommandResponse>.FailureResponse(ErrorCodes.WarehouseContactNotFound, "Warehouse contact not found.", 404);

            return ApiResponse<EditWarehouseContactCommandResponse>.SuccessResponse(mapper.Map<EditWarehouseContactCommandResponse>(result));
        }
    }
}
