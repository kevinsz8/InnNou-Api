namespace InnNou.Domain.Dtos
{
    public class FamilyApprovalThresholdDto
    {
        public int FamilyApprovalThresholdId { get; set; }
        public Guid FamilyApprovalThresholdToken { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string OrganizationName { get; set; } = default!;
        public int FamilyId { get; set; }
        public Guid FamilyToken { get; set; }
        public string FamilyCode { get; set; } = default!;
        public int Level { get; set; }
        public decimal ThresholdAmount { get; set; }
        public int ApproverUserId { get; set; }
        public Guid ApproverUserToken { get; set; }
        public string ApproverName { get; set; } = default!;
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
