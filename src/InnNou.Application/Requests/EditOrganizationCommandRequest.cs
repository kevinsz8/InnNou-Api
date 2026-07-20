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

        // Only valid for an Associate-type organization (enforced in OrganizationService) —
        // Super Asociado orgs are never zoned. Can be set, not cleared (same "supplied value
        // wins, otherwise unchanged" limitation every other nullable field on this entity has).
        public Guid? ZoneToken { get; set; }
    }
}
