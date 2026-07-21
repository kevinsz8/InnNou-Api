using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetPendingOrderApprovalsQueryHandler(IOrderService orderService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetPendingOrderApprovalsQueryRequest, ApiResponse<GetPendingOrderApprovalsQueryResponse>>
    {
        public async Task<ApiResponse<GetPendingOrderApprovalsQueryResponse>> Handle(GetPendingOrderApprovalsQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await orderService.GetPendingApprovalStepsAsync(request.PageNumber, request.PageSize, context, cancellationToken);
            var totalPages = result.TotalPages;
            var response = new GetPendingOrderApprovalsQueryResponse
            {
                OrderApprovalSteps = mapper.MapList<Responses.Common.OrderApprovalStep>(result.Items),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = totalPages,
                HasNextPage = result.PageNumber < totalPages,
                HasPreviousPage = result.PageNumber > 1,
                NextPageNumber = result.PageNumber < totalPages ? result.PageNumber + 1 : (int?)null,
                PreviousPageNumber = result.PageNumber > 1 ? result.PageNumber - 1 : (int?)null
            };
            return ApiResponse<GetPendingOrderApprovalsQueryResponse>.SuccessResponse(response);
        }
    }
}
