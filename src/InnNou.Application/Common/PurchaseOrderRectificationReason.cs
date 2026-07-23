namespace InnNou.Application.Common
{
    // Underlying int values must match PurchaseOrderRectificationReasons seed rows exactly
    // (see database/migrations/20260723_PurchaseOrderRectifications_Create.sql). Member names
    // preserve underscores to match Dapper's plain case-insensitive Enum.Parse against the Code
    // column — see OrderStatus.Pending_Approval's comment for the full explanation.
    public enum PurchaseOrderRectificationReason
    {
        Supplier_Stock_Shortage = 1,
        Price_Correction = 2,
        Quantity_Error = 3,
        Delivery_Issue = 4,
        Other = 5
    }

    public static class PurchaseOrderRectificationReasonCodes
    {
        public const string SupplierStockShortage = "SUPPLIER_STOCK_SHORTAGE";
        public const string PriceCorrection = "PRICE_CORRECTION";
        public const string QuantityError = "QUANTITY_ERROR";
        public const string DeliveryIssue = "DELIVERY_ISSUE";
        public const string Other = "OTHER";

        // Non-throwing variant for caller-supplied values (CreatePurchaseOrderRectificationCommandRequest.Reason)
        // — an unrecognized code is a 400 INVALID_REQUEST at the handler, never a 500.
        public static bool TryFromCode(string? code, out string normalizedCode)
        {
            switch (code?.Trim().ToUpperInvariant())
            {
                case SupplierStockShortage: normalizedCode = SupplierStockShortage; return true;
                case PriceCorrection: normalizedCode = PriceCorrection; return true;
                case QuantityError: normalizedCode = QuantityError; return true;
                case DeliveryIssue: normalizedCode = DeliveryIssue; return true;
                case Other: normalizedCode = Other; return true;
                default: normalizedCode = string.Empty; return false;
            }
        }
    }
}
