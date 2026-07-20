using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DeleteSupplierDeliveryZoneCommandRequest : IRequest<ApiResponse<DeleteSupplierDeliveryZoneCommandResponse>>
    {
        public Guid? SupplierToken { get; set; }
        public Guid ZoneToken { get; set; }
        public int DayOfWeek { get; set; }
    }
}
