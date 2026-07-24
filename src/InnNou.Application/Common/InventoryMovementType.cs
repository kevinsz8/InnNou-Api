namespace InnNou.Application.Common
{
    // Underlying int values must match InventoryMovementTypes.InventoryMovementTypeId seed rows
    // exactly (see database/migrations/20260727_Inventory_Create.sql).
    public enum InventoryMovementType
    {
        Receipt = 1,
        Adjustment = 2,
        Transfer_Out = 3,
        Transfer_In = 4
    }

    public static class InventoryMovementTypeCodes
    {
        public const string Receipt = "RECEIPT";
        public const string Adjustment = "ADJUSTMENT";
        public const string TransferOut = "TRANSFER_OUT";
        public const string TransferIn = "TRANSFER_IN";

        public static string ToCode(InventoryMovementType type) => type switch
        {
            InventoryMovementType.Receipt => Receipt,
            InventoryMovementType.Adjustment => Adjustment,
            InventoryMovementType.Transfer_Out => TransferOut,
            InventoryMovementType.Transfer_In => TransferIn,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static InventoryMovementType FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Receipt => InventoryMovementType.Receipt,
            Adjustment => InventoryMovementType.Adjustment,
            TransferOut => InventoryMovementType.Transfer_Out,
            TransferIn => InventoryMovementType.Transfer_In,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
