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
        // Denominated in UnitToken when provided (must resolve to the article's own
        // PurchaseUnitId or a level in its ArticlePackagingLevels chain — see
        // ArticleUnitConversion), or in the article's PurchaseUnitId directly when null. The
        // sign always matches DeltaQuantity's own increase(+)/decrease(-) convention.
        public Guid? UnitToken { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
