namespace InnNou.Application.Responses
{
    public class DeleteOrganizationCommandResponse
    {
        public Guid OrganizationToken { get; set; }
        public bool Success { get; set; }
    }
}
