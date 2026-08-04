using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateInternalOrderShipmentLineRequestItem
    {
        public Guid InternalOrderLineToken { get; set; }
        public decimal QuantityShipped { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateInternalOrderShipmentCommandRequest : IRequest<ApiResponse<CreateInternalOrderShipmentCommandResponse>>
    {
        public Guid InternalOrderToken { get; set; }
        public Guid SourceWarehouseToken { get; set; }
        public string? Notes { get; set; }
        public List<CreateInternalOrderShipmentLineRequestItem> Lines { get; set; } = [];
    }
}
