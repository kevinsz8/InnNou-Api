namespace InnNou.Application.Responses
{
    public class OrderApprovalEmailApproveResultResponse
    {
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public bool OrderFullyApproved { get; set; }
    }
}
