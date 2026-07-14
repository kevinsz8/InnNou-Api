using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class ExportSubCategoriesQueryRequest : IRequest<FileResult>
    {
        public string? Language { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeInactive { get; set; }
    }
}
