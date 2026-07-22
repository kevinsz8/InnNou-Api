using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateOrganizationCommandRequest : IRequest<ApiResponse<CreateOrganizationCommandResponse>>
    {
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public string? Code { get; set; }
        public int? ParentOrganizationId { get; set; }
        public int? OrganizationTypeId { get; set; }
        public string? TimeZone { get; set; }
        public string? CurrencyCode { get; set; }
        public string? LanguageCode { get; set; }

        // Only valid for an Associate-type organization (enforced in OrganizationService) —
        // Super Asociado orgs are never zoned.
        public Guid? ZoneToken { get; set; }

        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Description { get; set; }
    }
}
