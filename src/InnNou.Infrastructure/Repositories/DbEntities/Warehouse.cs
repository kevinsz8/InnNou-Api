namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class Warehouse
    {
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public int OrganizationId { get; set; }
        public string Name { get; set; } = default!;
        public string NormalizedName { get; set; } = default!;
        public string? Code { get; set; }
        public string? Description { get; set; }

        public bool IsInventoriable { get; set; }
        public bool CanReceivePurchases { get; set; }
        public bool CanReceiveTransfers { get; set; }
        public bool CanTransferOut { get; set; }
        public bool CanConsumeInventory { get; set; }
        public bool CanProduceItems { get; set; }
        public bool CanSellItems { get; set; }
        public bool CanAdjustInventory { get; set; }
        public bool CanReceiveReturns { get; set; }
        public bool TrackLotNumbers { get; set; }
        public bool TrackExpirationDates { get; set; }
        public bool TrackSerialNumbers { get; set; }
        public bool RequireApproval { get; set; }
        public bool IsDefaultReceivingWarehouse { get; set; }
        public bool IsDefaultConsumptionWarehouse { get; set; }
        public bool IsMainWarehouse { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? DeletedUtc { get; set; }
        public string? DeletedBy { get; set; }
    }
}
