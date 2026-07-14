using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class BulkImportOrganizationsCommandRequest : IRequest<ApiResponse<BulkImportOrganizationsCommandResponse>>
    {
        public required byte[] FileBytes { get; set; }
        public required string FileName { get; set; }
    }
}
