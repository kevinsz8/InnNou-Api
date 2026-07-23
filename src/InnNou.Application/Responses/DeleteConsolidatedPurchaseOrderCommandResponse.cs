namespace InnNou.Application.Responses
{
    public class DeleteConsolidatedPurchaseOrderCommandResponse
    {
        public Guid ConsolidatedPurchaseOrderToken { get; set; }
        public bool Success { get; set; }
    }
}
