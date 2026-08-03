using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetGoodsReceiptTaxPreviewQueryRequest : IRequest<ApiResponse<GetGoodsReceiptTaxPreviewQueryResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
    }
}
