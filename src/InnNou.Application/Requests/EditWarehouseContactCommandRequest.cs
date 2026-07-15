using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class EditWarehouseContactCommandRequest : IRequest<ApiResponse<EditWarehouseContactCommandResponse>>
    {
        public Guid WarehouseContactToken { get; set; }
        public string ContactName { get; set; } = default!;
        public string? ContactType { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public bool IsPrimary { get; set; }

        // Nullable so a caller who isn't touching system access can omit these entirely —
        // see WarehouseContactService.EditAsync's touchesAccess check.
        public bool? HasAccessToSystem { get; set; }
        public string? LoginEmail { get; set; }
        public string? Password { get; set; }
    }
}
