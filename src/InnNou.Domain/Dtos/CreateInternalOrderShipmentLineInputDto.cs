namespace InnNou.Domain.Dtos
{
    // One line dispatched as part of a single InternalOrderShipment create call. QuantityShipped
    // must be > 0 and cannot exceed the referenced InternalOrderLine's remaining-to-ship
    // (Quantity - already-shipped total) nor the source Warehouse's current on-hand balance.
    public class CreateInternalOrderShipmentLineInputDto
    {
        public Guid InternalOrderLineToken { get; set; }
        public decimal QuantityShipped { get; set; }
        public string? Notes { get; set; }
    }
}
