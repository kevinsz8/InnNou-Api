using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateSupplierDeliveryZoneCommandRequest : IRequest<ApiResponse<CreateSupplierDeliveryZoneCommandResponse>>
    {
        // Omitted ⇒ the caller's own supplier (context.SupplierId) — only Admin+ may supply this
        // to manage a different supplier's coverage.
        public Guid? SupplierToken { get; set; }
        public Guid ZoneToken { get; set; }

        // System.DayOfWeek convention: 0 = Sunday .. 6 = Saturday.
        public int DayOfWeek { get; set; }
    }
}
