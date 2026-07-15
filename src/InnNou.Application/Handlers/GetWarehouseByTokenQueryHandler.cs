using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetWarehouseByTokenQueryHandler(IWarehouseService warehouseService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetWarehouseByTokenQueryRequest, ApiResponse<GetWarehouseByTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetWarehouseByTokenQueryResponse>> Handle(GetWarehouseByTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var warehouse = await warehouseService.GetByTokenAsync(request.WarehouseToken, context, cancellationToken);
            if (warehouse is null)
                return ApiResponse<GetWarehouseByTokenQueryResponse>.FailureResponse(ErrorCodes.WarehouseNotFound, "Warehouse not found.", 404);

            return ApiResponse<GetWarehouseByTokenQueryResponse>.SuccessResponse(new GetWarehouseByTokenQueryResponse
            {
                Warehouse = mapper.Map<Responses.Common.Warehouse>(warehouse)
            });
        }
    }
}
