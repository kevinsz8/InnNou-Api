namespace InnNou.Domain.Dtos
{
    // Result of a successful anonymous email-token approval — enough for the confirmation page
    // to tell the difference between "this level is approved, more levels still pending" and
    // "every required level is now approved, the order is being finalized."
    public class OrderApprovalEmailApproveResultDto
    {
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public bool OrderFullyApproved { get; set; }
    }
}
