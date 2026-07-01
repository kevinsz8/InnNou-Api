using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditOrganizationCommandRequest : IRequest<ApiResponse<EditOrganizationCommandResponse>>
    {
        public Guid OrganizationToken { get; set; }
        public string? Name { get; set; }
        public string? LegalName { get; set; }
        public string? Code { get; set; }
        public int? ParentOrganizationId { get; set; }
        public int? OrganizationTypeId { get; set; }
        public string? TimeZone { get; set; }
        public string? CurrencyCode { get; set; }
        public string? LanguageCode { get; set; }
    }
}
