using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetGoodsReceiptsQueryRequest : IRequest<ApiResponse<GetGoodsReceiptsQueryResponse>>
    {
        public Guid? PurchaseOrderToken { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
