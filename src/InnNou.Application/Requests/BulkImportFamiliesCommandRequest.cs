using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class BulkImportFamiliesCommandRequest : IRequest<ApiResponse<BulkImportFamiliesCommandResponse>>
    {
        public required byte[] FileBytes { get; set; }
        public required string FileName { get; set; }
    }
}
