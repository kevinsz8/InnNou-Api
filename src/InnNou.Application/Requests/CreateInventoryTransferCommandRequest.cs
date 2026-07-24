using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateInventoryTransferLineRequestItem
    {
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateInventoryTransferCommandRequest : IRequest<ApiResponse<CreateInventoryTransferCommandResponse>>
    {
        public Guid FromWarehouseToken { get; set; }
        public Guid ToWarehouseToken { get; set; }
        public string? Notes { get; set; }
        public List<CreateInventoryTransferLineRequestItem> Lines { get; set; } = [];
    }
}
