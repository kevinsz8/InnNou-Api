using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetOrdersQueryHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetOrdersQueryRequest, ApiResponse<GetOrdersQueryResponse>>
    {
        public async Task<ApiResponse<GetOrdersQueryResponse>> Handle(GetOrdersQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await orderService.GetPagedAsync(
                request.WarehouseToken, request.Status, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetOrdersQueryResponse
            {
                Orders = mapper.MapList<Responses.Common.Order>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetOrdersQueryResponse>.SuccessResponse(response);
        }
    }
}
