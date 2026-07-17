using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetOrdersQueryRequest : IRequest<ApiResponse<GetOrdersQueryResponse>>
    {
        public Guid? WarehouseToken { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
