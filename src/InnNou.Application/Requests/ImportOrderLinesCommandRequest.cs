using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ImportOrderLinesCommandRequest : IRequest<ApiResponse<ImportOrderLinesCommandResponse>>
    {
        public Guid OrderToken { get; set; }
        public required byte[] FileBytes { get; set; }
        public required string FileName { get; set; }
    }
}
