using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetWarehouseContactsByWarehouseTokenQueryHandler(IWarehouseContactService warehouseContactService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetWarehouseContactsByWarehouseTokenQueryRequest, ApiResponse<GetWarehouseContactsByWarehouseTokenQueryResponse>>
    {
        public async Task<ApiResponse<GetWarehouseContactsByWarehouseTokenQueryResponse>> Handle(GetWarehouseContactsByWarehouseTokenQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await warehouseContactService.GetPagedByWarehouseTokenAsync(
                request.WarehouseToken, request.PageNumber, request.PageSize,
                request.SearchText, request.IncludeInactive, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetWarehouseContactsByWarehouseTokenQueryResponse
            {
                WarehouseContacts = mapper.MapList<Responses.Common.WarehouseContact>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetWarehouseContactsByWarehouseTokenQueryResponse>.SuccessResponse(response);
        }
    }
}
