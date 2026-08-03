namespace InnNou.Application.Common
{
    // Underlying int values must match PurchaseOrderRectificationLineActions seed rows exactly
    // (see database/migrations/20260723_PurchaseOrderRectifications_Create.sql). Member names
    // preserve underscores — see OrderStatus.Pending_Approval's comment for why.
    public enum PurchaseOrderRectificationLineAction
    {
        Quantity_Price_Change = 1,
        Line_Cancelled = 2,
        Line_Added = 3
    }

    public static class PurchaseOrderRectificationLineActionCodes
    {
        public const string QuantityPriceChange = "QUANTITY_PRICE_CHANGE";
        public const string LineCancelled = "LINE_CANCELLED";
        public const string LineAdded = "LINE_ADDED";

        public static string ToCode(PurchaseOrderRectificationLineAction action) => action switch
        {
            PurchaseOrderRectificationLineAction.Quantity_Price_Change => QuantityPriceChange,
            PurchaseOrderRectificationLineAction.Line_Cancelled => LineCancelled,
            PurchaseOrderRectificationLineAction.Line_Added => LineAdded,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
