namespace InnNou.Domain.Dtos
{
    // One line requested as part of a single Requisition create/add-line call. QuantityRequested
    // must be > 0.
    public class CreateRequisitionLineInputDto
    {
        public Guid ArticleToken { get; set; }
        public decimal QuantityRequested { get; set; }
        public string? Notes { get; set; }
    }
}
