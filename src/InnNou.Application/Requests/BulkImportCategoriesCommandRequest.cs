using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class BulkImportCategoriesCommandRequest : IRequest<ApiResponse<BulkImportCategoriesCommandResponse>>
    {
        public required byte[] FileBytes { get; set; }
        public required string FileName { get; set; }
    }
}
