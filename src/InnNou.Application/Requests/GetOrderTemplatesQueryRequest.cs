using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetOrderTemplatesQueryRequest : IRequest<ApiResponse<GetOrderTemplatesQueryResponse>>
    {
        public Guid? OrganizationToken { get; set; }
        public Guid? WarehouseToken { get; set; }
        public string? SearchText { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
