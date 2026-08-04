using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetInternalOrdersQueryRequest : IRequest<ApiResponse<GetInternalOrdersQueryResponse>>
    {
        // 'REQUESTING' narrows to "my requests", 'SOURCE' to "requests I need to fulfill" — the
        // frontend's two separate tabs/pages. Omitted shows both directions.
        public string? Direction { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
