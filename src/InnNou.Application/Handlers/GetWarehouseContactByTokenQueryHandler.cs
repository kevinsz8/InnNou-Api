using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetWarehouseContactByTokenQueryHandler(IWarehouseContactService warehouseContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetWarehouseContactByTokenQueryRequest, ApiResponse<GetWarehouseContactByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetWarehouseContactByTokenQueryResponse>> Handle(GetWarehouseContactByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var contact = await warehouseContactService.GetByTokenAsync(request.WarehouseContactToken, context, cancellationToken);
            if (contact is null)
                return ApiResponse<GetWarehouseContactByTokenQueryResponse>.FailureResponse(ErrorCodes.WarehouseContactNotFound, "Warehouse contact not found.", 404);

            return ApiResponse<GetWarehouseContactByTokenQueryResponse>.SuccessResponse(new GetWarehouseContactByTokenQueryResponse
            {
                WarehouseContact = mapper.Map<Responses.Common.WarehouseContact>(contact)
            });
        }
    }
}
