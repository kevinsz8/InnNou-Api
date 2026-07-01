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
        public bool HasAccessToSystem { get; set; }
        public string? LoginEmail { get; set; }
        public string? Password { get; set; }
    }
}
