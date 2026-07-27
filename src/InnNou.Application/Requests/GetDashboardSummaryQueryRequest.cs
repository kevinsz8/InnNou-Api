using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    // No params — always "my own scope" (the caller's own organization hierarchy, or
    // unrestricted for a SuperAdmin). Kept as a class, not an empty record, for consistency with
    // every other Request in this codebase and so a future date-range param can be added without
    // a breaking shape change.
    public class GetDashboardSummaryQueryRequest : IRequest<ApiResponse<GetDashboardSummaryQueryResponse>>
    {
    }
}
