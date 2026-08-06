using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class SubmitInventoryPeriodCountCommandRequest : IRequest<ApiResponse<SubmitInventoryPeriodCountCommandResponse>>
    {
        public Guid InventoryPeriodToken { get; set; }
        public Guid ArticleToken { get; set; }
        public decimal CountedQuantity { get; set; }
        // Denominated in UnitToken when provided (see ArticleUnitConversion), or in the
        // article's PurchaseUnitId directly when null.
        public Guid? UnitToken { get; set; }
    }
}
