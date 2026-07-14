using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetUserImportTemplateQueryRequest : IRequest<FileResult>
    {
        public string? Language { get; set; }
    }
}
