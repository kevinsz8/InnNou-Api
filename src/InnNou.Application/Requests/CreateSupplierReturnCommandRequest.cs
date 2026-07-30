using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateSupplierReturnCommandRequest : IRequest<ApiResponse<CreateSupplierReturnCommandResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
        public string? Notes { get; set; }
        public List<Guid> GoodsReceiptLineTokens { get; set; } = [];
    }
}
