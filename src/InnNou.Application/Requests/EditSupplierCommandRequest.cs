using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditSupplierCommandRequest : IRequest<ApiResponse<EditSupplierCommandResponse>>
    {
        public Guid SupplierToken { get; set; }
        public string? Name { get; set; }
        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public bool? IsGlobal { get; set; }
        public string? SupplierType { get; set; }
        public bool? HasAccessToSystem { get; set; }
        public string? LoginEmail { get; set; }
        public string? Password { get; set; }

        // Reassigns the owning organization when set and IsGlobal is (or stays) false.
        // Changing IsGlobal or this field is SuperAdmin-only regardless of who owns the
        // supplier today — see SupplierService.EditSupplierAsync.
        public Guid? OrganizationToken { get; set; }

        // Resubmit-with-confirm flag for the privatization-impact check (see
        // ErrorCodes.SupplierPrivatizationImpact) — set true only after the caller has seen
        // and accepted the impact counts from a prior rejected attempt.
        public bool ConfirmPrivatizationImpact { get; set; }
    }
}
