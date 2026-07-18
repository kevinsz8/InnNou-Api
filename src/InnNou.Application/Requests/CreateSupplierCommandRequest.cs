using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateSupplierCommandRequest : IRequest<ApiResponse<CreateSupplierCommandResponse>>
    {
        public string Name { get; set; } = default!;
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
        public bool IsGlobal { get; set; }
        public string? SupplierType { get; set; }
        public bool HasAccessToSystem { get; set; }
        public string? LoginEmail { get; set; }
        public string? Password { get; set; }

        // Target owning organization when IsGlobal is false. Required for a SuperAdmin caller
        // creating a private supplier; ignored for a Staff+ caller, who can only ever create a
        // private supplier for their own organization (server-resolved, never client-supplied).
        public Guid? OrganizationToken { get; set; }
    }
}
