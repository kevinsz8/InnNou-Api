using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetInternalOrdersQueryHandler(IInternalOrderService internalOrderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetInternalOrdersQueryRequest, ApiResponse<GetInternalOrdersQueryResponse>>
    {
        private static readonly HashSet<string> ValidDirections = new(StringComparer.OrdinalIgnoreCase) { "REQUESTING", "SOURCE" };

        public async Task<ApiResponse<GetInternalOrdersQueryResponse>> Handle(GetInternalOrdersQueryRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.Direction) && !ValidDirections.Contains(request.Direction))
                return ApiResponse<GetInternalOrdersQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Direction must be 'REQUESTING' or 'SOURCE'.", 400);

            if (!string.IsNullOrWhiteSpace(request.Status) && !InternalOrderStatusCodes.TryFromCode(request.Status, out _))
                return ApiResponse<GetInternalOrdersQueryResponse>.FailureResponse(ErrorCodes.InvalidRequest, "Invalid Status filter.", 400);

            var result = await internalOrderService.GetPagedAsync(
                request.Direction?.Trim().ToUpperInvariant(), request.Status, request.PageNumber, request.PageSize, context, cancellationToken);

            var totalPages = result.TotalPages;
            var response = new GetInternalOrdersQueryResponse
            {
                InternalOrders = mapper.MapList<Responses.Common.InternalOrder>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetInternalOrdersQueryResponse>.SuccessResponse(response);
        }
    }
}
