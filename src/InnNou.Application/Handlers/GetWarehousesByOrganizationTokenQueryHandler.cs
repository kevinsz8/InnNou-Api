using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetWarehousesByOrganizationTokenQueryHandler(IWarehouseService warehouseService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetWarehousesByOrganizationTokenQueryRequest, ApiResponse<GetWarehousesByOrganizationTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetWarehousesByOrganizationTokenQueryResponse>> Handle(GetWarehousesByOrganizationTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await warehouseService.GetPagedByOrganizationTokenAsync(
                request.OrganizationToken, request.PageNumber, request.PageSize,
                request.SearchText, request.IncludeInactive, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetWarehousesByOrganizationTokenQueryResponse
            {
                Warehouses = mapper.MapList<Responses.Common.Warehouse>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetWarehousesByOrganizationTokenQueryResponse>.SuccessResponse(response);
        }
    }
}
