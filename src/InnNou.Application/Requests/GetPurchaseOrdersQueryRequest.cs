using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetPurchaseOrdersQueryRequest : IRequest<ApiResponse<GetPurchaseOrdersQueryResponse>>
    {
        public Guid? OrganizationToken { get; set; }
        public Guid? OrderToken { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
