using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateInventoryAdjustmentCommandRequest : IRequest<ApiResponse<CreateInventoryAdjustmentCommandResponse>>
    {
        public Guid WarehouseToken { get; set; }
        public Guid ArticleToken { get; set; }
        public decimal DeltaQuantity { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
