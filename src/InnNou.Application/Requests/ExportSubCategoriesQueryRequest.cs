using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ExportSubCategoriesQueryRequest : IRequest<FileResult>
    {
        public string? SearchText { get; set; }
        public bool IncludeInactive { get; set; }
    }
}
