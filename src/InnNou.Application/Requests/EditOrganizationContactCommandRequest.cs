using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditOrganizationContactCommandRequest : IRequest<ApiResponse<EditOrganizationContactCommandResponse>>
    {
        public Guid OrganizationContactToken { get; set; }
        public string ContactName { get; set; } = default!;
        public string? ContactType { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }
    }
}
