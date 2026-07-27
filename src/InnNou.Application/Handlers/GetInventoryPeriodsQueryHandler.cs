using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInventoryPeriodsQueryHandler(IInventoryPeriodService inventoryPeriodService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetInventoryPeriodsQueryRequest, ApiResponse<GetInventoryPeriodsQueryResponse>>
    {
        public async Task<ApiResponse<GetInventoryPeriodsQueryResponse>> Handle(GetInventoryPeriodsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await inventoryPeriodService.GetPagedAsync(
                request.WarehouseToken, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetInventoryPeriodsQueryResponse
            {
                InventoryPeriods = mapper.MapList<Responses.Common.InventoryPeriod>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetInventoryPeriodsQueryResponse>.SuccessResponse(response);
        }
    }
}
