using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ExportOrderTemplateQueryRequest : IRequest<FileResult>
    {
        public Guid OrderTemplateToken { get; set; }
        public string? Language { get; set; }
    }
}
