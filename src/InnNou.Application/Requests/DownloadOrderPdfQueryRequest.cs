using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DownloadOrderPdfQueryRequest : IRequest<FileResult>
    {
        public Guid OrderToken { get; set; }
    }
}
