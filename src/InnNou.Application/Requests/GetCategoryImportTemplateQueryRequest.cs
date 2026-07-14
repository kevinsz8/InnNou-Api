using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class GetCategoryImportTemplateQueryRequest : IRequest<FileResult>
    {
        public string? Language { get; set; }
    }
}
