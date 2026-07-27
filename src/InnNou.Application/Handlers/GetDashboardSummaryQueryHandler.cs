using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Shared.Mapping;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class GetDashboardSummaryQueryHandler(IDashboardService dashboardService, IMapper mapper, IRequestContext context)
        : IRequestHandler<GetDashboardSummaryQueryRequest, ApiResponse<GetDashboardSummaryQueryResponse>>
    {
        public async Task<ApiResponse<GetDashboardSummaryQueryResponse>> Handle(GetDashboardSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            var result = await dashboardService.GetSummaryAsync(context, cancellationToken);

            return ApiResponse<GetDashboardSummaryQueryResponse>.SuccessResponse(new GetDashboardSummaryQueryResponse
            {
                Summary = mapper.Map<Responses.Common.DashboardSummary>(result)
            });
        }
    }
}
