namespace InnNou.Domain.Dtos
{
    // One line requested as part of a single InventoryTransfer create call. Quantity must be
    // > 0 and cannot exceed the FromWarehouse's current on-hand balance for that Article.
    public class CreateInventoryTransferLineInputDto
    {
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }
}
