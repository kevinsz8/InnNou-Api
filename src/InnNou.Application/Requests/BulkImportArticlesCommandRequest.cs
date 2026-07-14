using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class BulkImportArticlesCommandRequest : IRequest<ApiResponse<BulkImportArticlesCommandResponse>>
    {
        public required byte[] FileBytes { get; set; }
        public required string FileName { get; set; }
    }
}
